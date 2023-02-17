using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.Services
{
    public class StorageService : IStorageService
    {
        // note, don't use AddAsync
        // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbset-1.addasync?view=efcore-7.0#remarks

        protected ValcoinContext Db { get; private set; } = new ValcoinContext();

        /// <summary>
        /// Gets the last block in the chain. In the event there are two forks with the same blockNumber, it will return the first one it finds.
        /// </summary>
        /// <returns></returns>
        public async Task<ValcoinBlock> GetLastBlock()
        {
            uint? lastId = await Db.ValcoinBlocks.MaxAsync(b => (uint?)b.BlockNumber);
            return await Db.ValcoinBlocks.FirstOrDefaultAsync(b => b.BlockNumber == lastId);
        }

        /// <summary>
        /// Returns a block with the specified hash if it exists.
        /// </summary>
        /// <param name="blockHashAsString"></param>
        /// <returns></returns>
        public async Task<ValcoinBlock> GetBlock(string blockHashAsString)
        {
            return await Db.ValcoinBlocks.FirstOrDefaultAsync(b => b.BlockHashAsString == blockHashAsString);
        }

        public async Task AddBlock(ValcoinBlock block)
        {
            Db.Add(block);
            await Db.SaveChangesAsync();
        }

        public async Task<Transaction> GetTx(string txId)
        {
            return await Db.Transactions.FirstOrDefaultAsync(t => t.TxId == txId);
        }

        public async Task AddTxs(IEnumerable<Transaction> txs)
        {
            foreach (Transaction tx in txs)
            {
                Db.Add(tx);
            }
            await Db.SaveChangesAsync();
        }

        public async Task AddWallet(Wallet wallet)
        {
            Db.Add(wallet);
            await Db.SaveChangesAsync();
        }

        public async Task<Wallet> GetMyWallet()
        {
            return await Db.Wallets.FirstOrDefaultAsync(w => w.PublicKey != null);
        }

        public async Task AddClient(Client client)
        {
            Db.Add(client);
            await Db.SaveChangesAsync();
        }

        public async Task<List<Client>> GetClients()
        {
            return await Db.Clients.ToListAsync();
        }

        public async Task UpdateClient(Client client)
        {
            Db.Update(client);
            await Db.SaveChangesAsync();
        }
    }
}
