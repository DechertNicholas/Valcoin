using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.Services
{
    public interface IMiningService
    {
        public static bool MineBlocks { get; set; }
        public int HashSpeed { get; set; }
        public string Status { get; set; }
        public static ConcurrentBag<Transaction> TransactionPool { get; set; }

        public Task<string> Mine();
        public void PopulateWalletInfo() { }
        public void ComputeHashSpeed();
        public void SetDifficultyMask(int difficulty);
        public ValcoinBlock BuildGenesisBlock();
        public Transaction AssembleCoinbaseTransaction();
        public void AssembleCandidateBlock();
        public void FindValidHash();
        public Task<string> CommitBlock();
    }
}
