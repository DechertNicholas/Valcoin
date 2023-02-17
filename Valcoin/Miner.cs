using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
        internal static bool MineBlocks = false;
        private static readonly int Difficulty = 22; // this will remain static for the purposes of this application, but normally would auto-adjust over time
        private static byte[] DifficultyMask = new byte[32];
        private static readonly Stopwatch Stopwatch = new();
        private static readonly TimeSpan HashInterval = new(0, 0, 10);
        private static int HashCount = 0;
        private static ValcoinBlock CandidateBlock = new();
        private static Wallet MyWallet;

        public static int HashSpeed { get; set; } = 0;
        public static ConcurrentBag<Transaction> TransactionPool { get; set; } = new();

        public static async void Mine()
        {
            // setup wallet info
            PopulateWalletInfo();

            //SynchronizeChain();

            // how many 0 bits need to lead the SHA256 hash. 256 is max, which would be impossible.
            // a difficulty of 6 means the hash bits must start with "000000xxxxxx..."
            SetDifficultyMask(Difficulty);

            // used to calculate hash speed
            Stopwatch.Start();
            while (MineBlocks == true)
            {
                AssembleCandidateBlock();
                FindValidHash();
                await CommitBlock();
            }
            // cleanup on stop so that we have nice fresh metrics when started again
            HashSpeed = 0;
        }

        private static async void PopulateWalletInfo()
        {
            MyWallet = await new StorageService().GetMyWallet();
            // this should never be called, but exists as a safety.
            // the application should always open to the wallet page first and generate a wallet if none exist
            if (MyWallet == null) { throw new NullReferenceException("A wallet was not found in the database"); }
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
            // debugging serialization
            var unlockBytes = (byte[])new UnlockSignatureStruct(CandidateBlock.BlockNumber, MyWallet.PublicKey);
            var input = new TxInput()
            {
                PreviousTransactionId = new string('0', 64),
                PreviousOutputIndex = -1, //0xFFFFFFFF
                UnlockerPublicKey = MyWallet.PublicKey,
                UnlockSignature = MyWallet.SignData(unlockBytes)
            };

            var output = new TxOutput()
            {
                Amount = 50,
                LockSignature = MyWallet.AddressBytes
            };

            var validated = Wallet.VerifyData(unlockBytes, MyWallet.SignData(unlockBytes), MyWallet.PublicKey);

            return new Transaction(CandidateBlock.BlockNumber, new TxInput[] { input }, new TxOutput[] { output });
        }

        private static async void AssembleCandidateBlock()
        {
            // always get the last block from the db, as the NetworkService may have gotten new information from the network
            var lastBlock = await new StorageService().GetLastBlock();
            if (lastBlock == null)
            {
                // no blocks are in the database after sync, start a new chain
                CandidateBlock = BuildGenesisBlock();
            }
            else
            {
                CandidateBlock = new ValcoinBlock(lastBlock.BlockNumber + 1, lastBlock.BlockHash, 0, DateTime.UtcNow, Difficulty);
            }

            // add our coinbase payout to ourselves, and any other transactions in the transaction pool (max 31 others, 32 tx total per block)
            CandidateBlock.AddTx(AssembleCoinbaseTransaction());

            if (!TransactionPool.IsEmpty)
            {
                for (var i = 0; i < Math.Min(31, TransactionPool.Count); i++)
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
            while (!hashFound && MineBlocks == true)
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
                        hashFound = true;
                    }
                }
            }
        }

        private static async Task CommitBlock()
        {
            //var service = new StorageService();
            //await service.AddBlock(CandidateBlock);
            //await service.AddTxs(CandidateBlock.Transactions);
            await Task.Run(() => NetworkService.RelayData(CandidateBlock));
        }
    }
}