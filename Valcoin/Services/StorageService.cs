using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.Services
{
    internal class StorageService
    {
        // note, don't use AddAsync
        // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbset-1.addasync?view=efcore-7.0#remarks

        public ValcoinContext Db { get; private set; } = new ValcoinContext();

        public async Task<ValcoinBlock> GetLastBlock()
        {
            uint? lastId = await Db.ValcoinBlocks.MaxAsync(b => (uint?)b.BlockNumber);
            return await Db.ValcoinBlocks.FirstOrDefaultAsync(b => b.BlockNumber == lastId);
        }

        public async void AddBlock(ValcoinBlock block)
        {
            Db.Add(block);
            await Db.SaveChangesAsync();
        }

        public async void AddTxs(IEnumerable<Transaction> txs)
        {
            foreach (Transaction tx in txs)
            {
                Db.Add(tx);
            }
            await Db.SaveChangesAsync();
       }

        public async void AddWallet(Wallet wallet)
        {
            Db.Add(wallet);
            await Db.SaveChangesAsync();
        }

        public async Task<Wallet> GetMyWallet()
        {
            return await Db.Wallets.FirstOrDefaultAsync(w => w.PrivateKey != null);
        }

        public async void AddClient(Client client)
        {
            Db.Add(client);
            await Db.SaveChangesAsync();
        }

        public async Task<List<Client>> GetClients()
        {
            return await Db.Clients.ToListAsync();
        }

        public async void UpdateClient(Client client)
        {
            Db.Update(client);
            await Db.SaveChangesAsync();
        }
    }
}
