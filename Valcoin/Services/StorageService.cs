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
        public async Task<ValcoinBlock> GetLastMainChainBlock()
        {
            uint? lastId = await Db.ValcoinBlocks.MaxAsync(b => (uint?)b.BlockNumber);
            if (lastId == 1) // this only happens for the first block after the genesis block
                return Db.ValcoinBlocks.First(b => b.BlockNumber == lastId);

            var lastMainChainBlock = Db.ValcoinBlocks
                .Where(b => Db.ValcoinBlocks
                    .Where(b2 => b2.BlockNumber == lastId - 1)
                    .FirstOrDefault().NextBlockHash.SequenceEqual(b.BlockHash))
                .FirstOrDefault();

            return lastMainChainBlock;
        }

        public async Task<List<ValcoinBlock>> GetHighestBlock()
        {
            uint? lastId = await Db.ValcoinBlocks.MaxAsync(b => (uint?)b.BlockNumber);
            return Db.ValcoinBlocks.Where(b => b.BlockNumber == lastId).ToList();
        }

        /// <summary>
        /// Returns a block with the specified hash if it exists.
        /// </summary>
        /// <param name="blockId"></param>
        /// <returns></returns>
        public async Task<ValcoinBlock> GetBlock(string blockId)
        {
            return await Db.ValcoinBlocks.FirstOrDefaultAsync(b => b.BlockId == blockId);
        }

        //[Obsolete("Use ChainService instead of StorageService.")]
        public async Task AddBlock(ValcoinBlock block)
        {
            //var lastBlock = await GetLastMainChainBlock();
            //if (lastBlock != null && lastBlock.BlockNumber != block.BlockNumber) // if we have an orphan incoming, don't update any blocks. Just add and save.
            //{
            //    block.BlockHash.CopyTo(lastBlock.NextBlockHash, 0);
            //    Db.Update(lastBlock);
            //}
            Db.Add(block);
            await Db.SaveChangesAsync();
        }

        public async Task UpdateBlock(ValcoinBlock block)
        {
            Db.Update(block);
            await Db.SaveChangesAsync();
        }

        public async Task<Transaction> GetTx(string transactionId)
        {
            return await Db.Transactions.FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        /// <summary>
        /// Returns a transaction that spends the output referenced by an input.
        /// </summary>
        /// <param name="txId"></param>
        /// <param name="outputIndex"></param>
        /// <returns></returns>
        public async Task<Transaction> GetTxByInput(string previousTransactionId, int outputIndex)
        {
            return await Db.Transactions
                .Where(t => t.Inputs                                                // iterate over transactions where the transaction has an input,
                    .Where(i => i.PreviousTransactionId == previousTransactionId)   // and that input references the previous txId,
                    .Where(i => i.PreviousOutputIndex == outputIndex)               // and the same outputIndex,
                    .FirstOrDefault().TransactionId == t.TransactionId)             // get that Input's transaction id, and match it to the transactions list
                .FirstOrDefaultAsync();                                             // and return that transaction
        }

        public async Task AddTxs(IEnumerable<Transaction> transactions)
        {
            foreach (Transaction tx in transactions)
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

        public async Task UpdateWallet(Wallet wallet)
        {
            Db.Wallets.Update(wallet);
            await Db.SaveChangesAsync();
        }

        public int GetMyBalance()
        {
            return Db.Wallets.First().Balance;
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
