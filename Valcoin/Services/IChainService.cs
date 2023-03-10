using Microsoft.UI.Xaml;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.Services
{
    public interface IChainService
    {
        public abstract IChainService GetFreshService();
        public Task<ValcoinBlock> GetLastMainChainBlock();
        public Task<ValcoinBlock> GetBlock(string blockId);
        public Task<List<ValcoinBlock>> GetBlocksByNumber(ulong blockNumber);
        public Task<List<ValcoinBlock>> GetAllBlocks();
        public Task AddBlock(ValcoinBlock block);
        public Task UpdateBlock(ValcoinBlock block);
        public Task AddPendingTransaction(Transaction tx);
        public Task CommitPendingTransaction(PendingTransaction px);
        public Task UnloadPendingTransactions(ulong blockNumber, int pendingTransactionTimeout);
        public Task<Transaction> GetTx(string transactionId);
        public Task<Transaction> GetTxByInput(string previousTransactionId, int outputIndex);
        public Task AddTxs(IEnumerable<Transaction> txs);
        public Task AddWallet(Wallet wallet);
        public Task UpdateWallet(Wallet wallet);
        public Task<Wallet> GetMyWallet();
        public Task<int> GetMyBalance();
        public Task AddClient(Client client);
        public Task<List<Client>> GetClients();
        public Task UpdateClient(Client client);
        public Task Transact(string recipient, int amount);
    }
}
