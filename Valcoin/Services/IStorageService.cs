using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.Services
{
    public interface IStorageService
    {
        public Task<ValcoinBlock> GetLastBlock();

        public Task<ValcoinBlock> GetBlock(string blockHashAsString);

        public Task AddBlock(ValcoinBlock block);

        public Task<Transaction> GetTx(string txId);
        public Task<Transaction> GetTxByInput(string previousTransactionId, int outputIndex);

        public Task AddTxs(IEnumerable<Transaction> txs);

        public Task AddWallet(Wallet wallet);

        public Task<Wallet> GetMyWallet();

        public Task AddClient(Client client);

        public Task<List<Client>> GetClients();

        public Task UpdateClient(Client client);
    }
}
