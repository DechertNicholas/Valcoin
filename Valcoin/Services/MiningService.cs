using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Valcoin.Helpers;
using Valcoin.Models;

namespace Valcoin.Services
{
    public class MiningService : IMiningService
    {
        public static bool MineBlocks { get; set; } = false;
        private readonly int Difficulty = 20; // this will remain static for the purposes of this application, but normally would auto-adjust over time
        private byte[] DifficultyMask = new byte[32];
        private readonly Stopwatch Stopwatch = new();
        private readonly TimeSpan HashInterval = new(0, 0, 2);
        private int HashCount = 0;
        private ValcoinBlock CandidateBlock = new();
        private Wallet MyWallet;
        private IChainService chainService;
        private INetworkService networkService;
        /// <summary>
        /// The number of blocks to be mined before a pending transaction is considered failed and removed from the pending database.
        /// Essentially, this transaction was not picked up by the mining network and can be re-attempted by the user.
        /// </summary>
        private int pendingTransactionTimeout = 3;

        public string Status { get; set; } = "Stopped";
        public int HashSpeed { get; set; } = 0;
        public static ConcurrentDictionary<string, Transaction> TransactionPool { get; set; } = new();

        public MiningService(IChainService chainService, INetworkService networkService)
        {
            this.chainService = chainService;
            this.networkService = networkService;
        }

        public async Task<string> Mine()
        {
            Thread.CurrentThread.Name = "Mining Thread";
            Status = "Mining";
            // setup wallet info
            PopulateWalletInfo();

            // how many 0 bits need to lead the SHA256 hash. 256 is max, which would be impossible.
            // a difficulty of 6 means the hash bits must start with "000000xxxxxx..."
            SetDifficultyMask(Difficulty);

            

            // used to calculate hash speed
            Stopwatch.Start();
            while (MineBlocks == true)
            {
                AssembleCandidateBlock();
                FindValidHash();
                if (MineBlocks == false) // when we stop mining, the hashing process stops and we need to not try to commit a block
                    return string.Empty; // no errors

                string errorPath;
                if ((errorPath = await CommitBlock()) != string.Empty)
                {
                    return errorPath;
                }
                await UnloadPendingTransactions();
            }
            Status = "Stopped";
            // cleanup on stop so that we have nice fresh metrics when started again
            HashSpeed = 0;
            return string.Empty;
        }

        public async void PopulateWalletInfo()
        {
            MyWallet = await chainService.GetMyWallet();
            // this should never be called, but exists as a safety.
            // the application should always open to the wallet page first and generate a wallet if none exist
            if (MyWallet == null) { throw new NullReferenceException("A wallet was not found in the database"); }
        }

        public async Task UnloadPendingTransactions()
        {
            await chainService.UnloadPendingTransactions(CandidateBlock.BlockNumber, pendingTransactionTimeout);
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
            // no value for UnlockSignature, as it will be filled in during the signing process
            var input = new TxInput(new string('0', 64), -1, MyWallet.PublicKey);
            input.UnlockSignature = BitConverter.GetBytes(CandidateBlock.BlockNumber); // coinbase needs to be unique, add the block number here

            var output = new TxOutput(50, MyWallet.AddressBytes);

            var tx = new Transaction(CandidateBlock.BlockNumber, new List<TxInput> { input }, new List<TxOutput> { output });
            MyWallet.SignTransactionInputs(ref tx);

            return tx;
        }

        public async void AssembleCandidateBlock()
        {
            // always get the last block from the db, as the NetworkService may have gotten new information from the network
            var lastBlock = await chainService.GetLastMainChainBlock();
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
                // only allow 32 transactions in a block, including the coinbase (so 31 from the pool)
                for (var i = 0; i < Math.Min(32 - CandidateBlock.Transactions.Count, TransactionPool.Count); i++)
                {
                    if (TransactionPool.Remove(TransactionPool.First().Key, out var tx))
                    {
                        tx.BlockNumber = CandidateBlock.BlockNumber;
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

        public async Task<string> CommitBlock()
        {
            // run our own block through validations before saving
            var valid = ValidationService.ValidateBlock(CandidateBlock);
            if (valid == ValidationService.ValidationCode.Valid)
            {
                await chainService.AddBlock(CandidateBlock);
                await networkService.RelayData(new Message(CandidateBlock));
                return "";
            }
            else
            {
                var path = WriteBlockToFile(CandidateBlock);
                MineBlocks = false;
                return path;
            }
        }

        private static string WriteBlockToFile(ValcoinBlock block)
        {
            var fileName = Windows.Storage.ApplicationData.Current.LocalFolder.Path + $"\\{block.BlockId}.txt";
            List<string> data = new()
            {
                "*** Block Info ***",
                $"BlockId: {block.BlockId}",
                $"BlockNumber: {block.BlockNumber}",
                $"BlockHash: {Convert.ToHexString(block.BlockHash)}",
                $"PreviousBlockHash: {Convert.ToHexString(block.PreviousBlockHash)}",
                $"NextBlockHash: {Convert.ToHexString(block.NextBlockHash)}",
                $"Nonce: {block.Nonce}",
                $"TimeUTCTicks: {block.TimeUTCTicks}",
                $"BlockDifficulty: {block.BlockDifficulty}",
                $"MerkleRoot: {Convert.ToHexString(block.MerkleRoot)}",
                $"Version: {block.Version}"
            };

            foreach (var tx in block.Transactions)
            {
                List<string> txData = new()
                {
                    "\n\n",
                    $"*** Transaction Info - {tx.TransactionId} ***",
                    $"TransactionId: {tx.TransactionId}",
                    $"Version: {tx.Version}",
                    $"BlockNumber: {tx.BlockNumber}"
                };

                txData.ForEach(s => data.Add(s));

                foreach (var input in tx.Inputs)
                {
                    List<string> inputData = new()
                    {
                        "\n\n",
                        $"*** TxInput Info - {input.PreviousTransactionId}***",
                        $"PreviousTransactionId: {input.PreviousTransactionId}",
                        $"PreviousOutputIndex: {input.PreviousOutputIndex}",
                        $"UnlockerPublicKey: {Convert.ToHexString(input.UnlockerPublicKey)}",
                        $"UnlockSignature: {Convert.ToHexString(input.UnlockSignature)}",
                        $"TransactionId: {input.TransactionId}"
                    };

                    inputData.ForEach(s => data.Add(s));
                }

                foreach (var output in tx.Outputs)
                {
                    List<string> outputData = new()
                    {
                        "\n\n",
                        $"*** TxOutput Info***",
                        $"Amount: {output.Amount}",
                        $"Address: {Convert.ToHexString(output.Address)}",
                        $"TransactionId: {output.TransactionId}"
                    };

                    outputData.ForEach(s => data.Add(s));
                }
            }

            File.WriteAllLines(fileName, data);
            return fileName;
        }
    }
}