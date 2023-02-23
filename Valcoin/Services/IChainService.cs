using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.Services
{
    public interface IChainService
    {
        public Task<ValcoinBlock> GetLastMainChainBlock();
        public Task<ValcoinBlock> GetBlock(string blockId);
        public Task<Transaction> GetTx(string transactionId);
        public Task<Transaction> GetTxByInput(string previousTransactionId, int outputIndex);
        public Task AddBlock(ValcoinBlock block);
    }
}
