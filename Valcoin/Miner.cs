using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;

namespace Valcoin
{
    public static class Miner
    {
        internal static bool Stop = false;
        private static readonly int Difficulty = 22;
        private static byte[] DifficultyMask = new byte[32];
        private static readonly Stopwatch Stopwatch = new();
        private static readonly TimeSpan HashInterval = new(0, 0, 10);
        private static int HashCount = 0;
        private static ValcoinBlock CandidateBlock = new();
        // LastBlock is mainly here to avoid EFCore from tracking the same object twice:
        // once while being loaded by GetLastBlock(), and once while being sent to the network
        // with Relay()
        private static ValcoinBlock LastBlock;
        private static Wallet MyWallet;

        public static int HashSpeed { get; set; } = 0;
        public static ConcurrentBag<Transaction> TransactionPool { get; set; } = new();

        public static void Mine()
        {
            // setup wallet info
            PopulateWalletInfo();

            //SynchronizeChain();

            // how many 0 bits need to lead the SHA256 hash. 256 is max, which would be impossible.
            // a difficulty of 6 means the hash bits must start with "000000xxxxxx..."
            SetDifficultyMask(Difficulty); // TODO: get this from the network

            // used to calculate hash speed
            Stopwatch.Start();
            while (Stop == false)
            {
                AssembleCandidateBlock();
                FindValidHash();
            }
            // cleanup on stop so that we have nice fresh metrics when started again
            HashSpeed = 0;
        }

        private static void PopulateWalletInfo()
        {
            MyWallet = StorageService.GetMyWallet();
        }

        private static void ComputeHashSpeed()
        {
            HashSpeed = HashCount / HashInterval.Seconds;

            // reset metrics
            HashCount = 0;
            Stopwatch.Restart();
        }

        private static void SetDifficultyMask(int difficulty)
        {
            int bytesToShift = Convert.ToInt32(Math.Ceiling(difficulty / 8d)); // 8 bits in a byte

            var difficultyMask = new byte[Convert.ToInt32(bytesToShift)];

            // fill 0s
            // bytesToShift - 1, because we don't want to fill the last byte
            for (var i = 0; i < bytesToShift - 1; i++)
            {
                difficultyMask[i] = 0x00;
            }

            int toShift = difficulty - (8 * (bytesToShift - 1));
            difficultyMask[^1] = (byte)(0b_1111_1111 >> toShift);
            DifficultyMask = difficultyMask;
        }

        private static ValcoinBlock BuildGenesisBlock()
        {
            var genesisHash = new byte[32];
            for (var i = 0; i < genesisHash.Length; i++)
            {
                genesisHash[i] = 0x00; //TODO: hash this to something other than straight 0's
            }
            return new ValcoinBlock(1, genesisHash, 0, DateTime.UtcNow, Difficulty);
        }

        private static Transaction AssembleCoinbaseTransaction()
        {
            var input = new TxInput()
            {
                PreviousTransactionId = new string('0', 64),
                PreviousOutputIndex = -1, //0xFFFFFFFF
                UnlockerPublicKey = MyWallet.PublicKey,
                UnlockSignature = MyWallet.SignData(new UnlockSignatureStruct { BlockNumber = CandidateBlock.BlockNumber, PublicKey = MyWallet.PublicKey })
            };

            var output = new TxOutput()
            {
                Amount = 50,
                LockSignature = MyWallet.AddressBytes
            };

            return new Transaction(CandidateBlock.BlockNumber, new TxInput[] { input }, new TxOutput[] { output });
        }

        private static void AssembleCandidateBlock()
        {
            // on first run, LastBlock will be null regardless. However it can also be null if the DB is new, so we check twice
            LastBlock ??= StorageService.GetLastBlock();
            if (LastBlock == null)
            {
                // no blocks are in the database after sync, start a new chain
                CandidateBlock = BuildGenesisBlock();
            }
            else
            {
                CandidateBlock = new ValcoinBlock(LastBlock.BlockNumber + 1, LastBlock.BlockHash, 0, DateTime.UtcNow, Difficulty);
            }

            // TODO: select transactions, condense the root
            CandidateBlock.AddTx(AssembleCoinbaseTransaction());

            if (!TransactionPool.IsEmpty)
            {
                for (var i = 0; i < Math.Min(32, TransactionPool.Count); i++)
                {
                    if (TransactionPool.TryTake(out Transaction tx))
                    {
                        CandidateBlock.AddTx(tx);
                    }
                }
            }
            
        }

        private static void FindValidHash()
        {
            var hashFound = false;
            // check on each hash if a stop has been requested
            while (!hashFound && Stop == false)
            {
                // update every 10 seconds
                if (Stopwatch.Elapsed >= HashInterval)
                    ComputeHashSpeed();

                CandidateBlock.ComputeAndSetHash();
                HashCount++;
                for (int i = 0; i < DifficultyMask.Length; i++)
                {
                    if (CandidateBlock.BlockHash[i] > DifficultyMask[i])
                    {
                        // didn't get the hash, try new nonce
                        CandidateBlock.Nonce++;
                        break;
                    }
                    else if (i == DifficultyMask.Length - 1)
                    {
                        CommitBlock();
                        hashFound = true;
                    }
                }
            }
        }

        private static void CommitBlock()
        {
            StorageService.AddBlock(CandidateBlock);
            StorageService.AddTxs(CandidateBlock.Transactions);
            Task.Run(() => NetworkService.RelayData(CandidateBlock));
        }
    }
}