﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.Services
{
    /// <summary>
    /// The chain service sits just above the <see cref="StorageService"/>, and handles data linking and <see cref="Wallet"/> balance.
    /// Anything directly related to the blockchain such as <see cref="ValcoinBlock"/>s or <see cref="Transaction"/>s should pass through here and not the
    /// <see cref="StorageService"/>.
    /// </summary>
    public class ChainService : IChainService
    {
        protected ValcoinContext Db { get; private set; }

        private byte[] myAddress;
        private byte[] myPublicKey;

        public ChainService(ValcoinContext context)
        {
            Db = context;
        }

        /// <summary>
        /// This gets a fresh context and is mainly used by the network
        /// </summary>
        /// <returns></returns>
        public IChainService GetFreshService()
        {
            return new ChainService(new ValcoinContext());
        }

        public virtual async Task<ValcoinBlock> GetLastMainChainBlock()
        {
            uint? lastId = await Db.ValcoinBlocks.MaxAsync(b => (uint?)b.BlockNumber);
            if (lastId == 1) // this only happens for the first block after the genesis block
                return Db.ValcoinBlocks.First(b => b.BlockNumber == lastId);

            // there's probably a fancy LINQ statement for this, but I couldn't get one to work
            foreach (var b in Db.ValcoinBlocks)
            {
                if (b.BlockNumber == lastId)
                {
                    foreach (var b2 in Db.ValcoinBlocks)
                    {
                        if (b2.NextBlockHash.SequenceEqual(b.BlockHash))
                        {
                            return b;
                        }
                    }
                }
            }

            // no blocks were found
            return (ValcoinBlock)null;
        }

        public async Task<ValcoinBlock> GetBlock(string blockId)
        {
            return await Db.ValcoinBlocks.FirstOrDefaultAsync(b => b.BlockId == blockId);
        }

        public async Task<List<ValcoinBlock>> GetBlocksByNumber(ulong blockNumber)
        {
            return await Db.ValcoinBlocks.Where(b => b.BlockNumber == blockNumber).ToListAsync();
        }

        public async Task<Transaction> GetTx(string transactionId)
        {
            return await Db.Transactions.FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

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

        public async Task AddBlock(ValcoinBlock block)
        {
            var lastBlock = await GetLastMainChainBlock();

            if (lastBlock == null && block.BlockNumber == 1)
            {
                // this is the genesis block being added
                await UpdateBalance(block);
                await CommitBlock(block);
                return;
            }

            // add a normal block to the chain. check that the lastBlock's number is one less than the incoming, and that the
            // new block references the last block
            if (lastBlock.BlockNumber == block.BlockNumber - 1 && lastBlock.BlockHash.SequenceEqual(block.PreviousBlockHash))
            {
                await UpdateBalance(block);
                // update the next newHighestBlock identifier
                block.BlockHash.CopyTo(lastBlock.NextBlockHash, 0);
                await UpdateBlock(lastBlock);
                await CommitBlock(block);
            }
            // check if this newHighestBlock is part of a longer blockchain
            else if (block.PreviousBlockHash != lastBlock.BlockHash && block.BlockNumber == lastBlock.BlockNumber + 1)
                await Reorganize(block);
            else
            {
                // this is some other block from the network, possibly an orphan or some other block we requested
                await CommitBlock(block);
            }
        }

        public async Task UpdateBlock(ValcoinBlock block)
        {
            Db.Update(block);
            await Db.SaveChangesAsync();
        }

        /// <summary>
        /// Saves a block to the database raw, without any additional checking or changes.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public virtual async Task CommitBlock(ValcoinBlock block)
        {
            Db.Add(block);
            await Db.SaveChangesAsync();
        }

        public virtual async Task UpdateBalance(ValcoinBlock block)
        {
            myAddress ??= (await GetMyWallet()).AddressBytes;
            myPublicKey ??= (await GetMyWallet()).PublicKey;
            // add any payments we may have gotten
            block.Transactions
                .ForEach(t => t.Outputs
                    .Where(o => o.LockSignature.SequenceEqual(myAddress) == true)
                    .ToList()
                    .ForEach(async o => await AddToBalance(o.Amount)));

            // subtract any payments we spent
            block.Transactions
                .Where(t => t.Inputs[0].PreviousTransactionId != new string('0', 64)) // filter out coinbase (this transaction is verified)
                .ToList()
                .ForEach(t => t.Inputs
                    .Where(i => i.UnlockerPublicKey.SequenceEqual(myPublicKey) == true)
                    .ToList()
                    .ForEach(async i => await SubtractFromBalance(t.Outputs.Sum(o => o.Amount))));
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

        public async Task<int> GetMyBalance()
        {
            return (await Db.Wallets.FirstAsync()).Balance;
        }

        private async Task AddToBalance(int payment)
        {
            var wallet = await GetMyWallet();
            wallet.Balance += payment;
            await UpdateWallet(wallet);
        }

        private async Task SubtractFromBalance(int payment)
        {
            var wallet = await GetMyWallet();
            wallet.Balance -= payment;
            await UpdateWallet(wallet);
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

        /// <summary>
        /// Reorganize the blockchain under the new highest block.
        /// </summary>
        /// <param name="newHighestBlock">The new highest block in the chain to reorganize under.</param>
        private async Task Reorganize(ValcoinBlock newHighestBlock)
        {
            /*
             * It is easier to visualize what is going on here than to try to just write it down.
             * There will be times when the network service will receive a block that has the same block number (position in the chain)
             * as a block we already have. In other cryptocurrencies, this is called an orphan or an uncle - we'll use orphan here
             * since it will be more true for our methods. Normally by the time we receive this block, we will have already started
             * mining (if we're mining) the next block. Essentially, we ignore this block in the chain, but still keep it in the database
             * in case its chain becomes longer:
             * 
             * Block:       1 2 3 4 5
             *                  ⌌-□ <- new block that we're ignoring (an orphan)
             * our chain -> ■-■-■-■-▣ <- the block we're mining
             * 
             * In the above diagram, the block we're mining has not been completed yet. This means there is essentially a race now
             * between our candidate block and a new block linking to the orphan block. Because of network latency, some other miners
             * likely received the orphan block before they received our block. Those miners will be working on the orphan chain. If
             * we win the race (our candidate block is mined and relayed to the network), the other miners will have to reorganize their
             * chains under our new block (using this Reorganize method). If we lose, and they commit the new block before we do, then we
             * have to reorganize:
             * 
             * Block:       1 2 3 4 5
             *                  ⌌-■-■ <- new highest block in the chain, verified and committed
             * our chain -> ■-■-■x□-▣ <- the block we were mining, which was not finished and will be abandoned (transactions sent back to the pool).
             *                     \new orphan
             * 
             * Now, we need to point block 3 to the previously orphaned block in the chain as the next in chain, leaving our "original"
             * block 4 (note the 4, not 3) as the new orphan. The transactions contained in that block will be read and released back to the transaction pool
             * to be re-processed (provided they were not processed in the new block 4). This prevents those transactions from being lost due to a reorganization,
             * at the cost of taking a little longer to process (again, provided the new block 4 did not already process them).
             * 
             * By having this method called, we already know the new block has been verified and is from the orphan chain, so we have no need to re-verify here.
             */

            var previousOrphan = await GetBlock(Convert.ToHexString(newHighestBlock.PreviousBlockHash));
            // the branch block is the block which had two different block referring back to it (the main chain and the orphan chain)
            var branchBlock = await GetBlock(Convert.ToHexString(previousOrphan.PreviousBlockHash));
            // the new orphan is the block that was previously in the main chain, that we are now disconnecting
            var newOrphan = await GetBlock(Convert.ToHexString(branchBlock.NextBlockHash));
            var txsToReRelease = newOrphan.Transactions;

            // now, reorganize the structure
            branchBlock.NextBlockHash = previousOrphan.BlockHash; // our new orphan is now disconnected from the chain
            // remove any transactions from our list that were already processed in the new two blocks
            previousOrphan.Transactions
                .ForEach(t => txsToReRelease
                    .Where(r => r.TransactionId == t.TransactionId)
                    .ToList()
                    .ForEach(x => txsToReRelease.Remove(x)));

            // do the same for the new highest block
            newHighestBlock.Transactions
                .ForEach(t => txsToReRelease
                    .Where(r => r.TransactionId == t.TransactionId)
                    .ToList()
                    .ForEach(x => txsToReRelease.Remove(x)));

            // TODO: Need to subtract our balance from any transactions that are being re-released, as we don't actually have that money yet

            // add the remaining transactions to the pool for the miner. There should be no duplicates, but just in case, check
            txsToReRelease.ForEach(t => GetTransactionPool()
                .Where(p => p.TransactionId != t.TransactionId)
                .ToList()
                .ForEach(r => AddToTransactionPool(r)));

            // update our branch block
            await UpdateBlock(branchBlock);

            // the previous orphan was already in the database, so we only need to update the NextBlockHash property
            previousOrphan.NextBlockHash = newHighestBlock.BlockHash;
            await UpdateBlock(previousOrphan);
            // the new orphan block is now an orphan (because branchBlock does not point to it) and there is no need
            // to perform any operations on it (other than having gotten the list of transactions).

            // now add the newHighestBlock
            await CommitBlock(newHighestBlock);
        }

        public List<Transaction> GetTransactionPool()
        {
            return MiningService.TransactionPool.ToList();
        }

        public void AddToTransactionPool(Transaction t)
        {
            MiningService.TransactionPool.Add(t);
        }
    }
}
