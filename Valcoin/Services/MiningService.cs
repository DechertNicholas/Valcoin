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

namespace Valcoin.Services
{
    public class MiningService : IMiningService
    {
        public bool MineBlocks { get; set; } = false;
        private readonly int Difficulty = 22; // this will remain static for the purposes of this application, but normally would auto-adjust over time
        private byte[] DifficultyMask = new byte[32];
        private readonly Stopwatch Stopwatch = new();
        private readonly TimeSpan HashInterval = new(0, 0, 10);
        private int HashCount = 0;
        private ValcoinBlock CandidateBlock = new();
        private Wallet MyWallet;

        public string Status { get; set; } = "Stopped";
        public int HashSpeed { get; set; } = 0;
        public ConcurrentBag<Transaction> TransactionPool { get; set; } = new();

        public async void Mine()
        {
            Status = "Mining";
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
            Status = "Stopped";
            // cleanup on stop so that we have nice fresh metrics when started again
            HashSpeed = 0;
        }

        public async void PopulateWalletInfo()
        {
            MyWallet = await new StorageService().GetMyWallet();
            // this should never be called, but exists as a safety.
            // the application should always open to the wallet page first and generate a wallet if none exist
            if (MyWallet == null) { throw new NullReferenceException("A wallet was not found in the database"); }
        }

        public void ComputeHashSpeed()
        {
            HashSpeed = HashCount / HashInterval.Seconds;

            // reset metrics
            HashCount = 0;
            Stopwatch.Restart();
        }

        public void SetDifficultyMask(int difficulty)
        {

            int bytesToShift = Convert.ToInt32(Math.Ceiling(difficulty / 8d)); // 8 bits in a byte

            var difficultyMask = new byte[Convert.ToInt32(bytesToShift)];

            // fill 0s
            // bytesToShift - 1, because we don't want to fill the last byte
            for (var i = 0; i < bytesToShift - 1; i++)
            {
                difficultyMask[i] = 0x00;
            }

            int toShift = difficulty - 8 * (bytesToShift - 1);
            difficultyMask[^1] = (byte)(0b_1111_1111 >> toShift);
            DifficultyMask = difficultyMask;
        }

        public ValcoinBlock BuildGenesisBlock()
        {
            var genesisHash = new byte[32];
            for (var i = 0; i < genesisHash.Length; i++)
            {
                genesisHash[i] = 0x00; //TODO: hash this to something other than straight 0's
            }
            return new ValcoinBlock(1, genesisHash, 0, DateTime.UtcNow.Ticks, Difficulty);
        }

        public Transaction AssembleCoinbaseTransaction()
        {
            // debugging serialization
            var unlockBytes = (byte[])new UnlockSignatureStruct(CandidateBlock.BlockNumber, MyWallet.PublicKey);
            var input = new TxInput(new string('0', 64), -1, MyWallet.PublicKey, MyWallet.SignData(unlockBytes));

            var output = new TxOutput(50, MyWallet.AddressBytes);

            return new Transaction(CandidateBlock.BlockNumber, new List<TxInput> { input }, new List<TxOutput> { output });
        }

        public async void AssembleCandidateBlock()
        {
            // always get the last block from the db, as the NetworkService may have gotten new information from the network
            var lastBlock = await ChainService.GetLastMainChainBlock();
            if (lastBlock == null)
            {
                // no blocks are in the database after sync, start a new chain
                CandidateBlock = BuildGenesisBlock();
            }
            else
            {
                CandidateBlock = new ValcoinBlock(lastBlock.BlockNumber + 1, lastBlock.BlockHash, 0, DateTime.UtcNow.Ticks, Difficulty);
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

        public void FindValidHash()
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

        public async Task CommitBlock()
        {
            // run our own block through validations before saving
            var valid = ValidationService.ValidateBlock(CandidateBlock);
            if (valid == ValidationService.ValidationCode.Valid)
            {
                await ChainService.AddBlock(CandidateBlock, new StorageService());
                await Task.Run(() => NetworkService.RelayData(CandidateBlock));
            }
            else
            {
                throw new InvalidOperationException($"Validation service returned {valid}");
            }
        }
    }
}