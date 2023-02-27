﻿using Microsoft.UI.Xaml;
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
        public Task AddBlock(ValcoinBlock block);
        public Task UpdateBlock(ValcoinBlock block);
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
    }
}