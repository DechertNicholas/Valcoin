using Microsoft.EntityFrameworkCore;
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

        private Wallet myWallet;
        private static bool reorganizing = false;
        private static readonly object padlock = new();

        public ChainService(ValcoinContext context)
        {
            Db = context;
            myWallet = GetMyWallet().Result;
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
            uint? highestBlockNumber = await Db.ValcoinBlocks.MaxAsync(b => (uint?)b.BlockNumber);
            // try to first find any block at that hight with a linked next block
            ValcoinBlock? highestBlock = Db.ValcoinBlocks.Where(b => b.BlockNumber == highestBlockNumber)
                .Where(b => !b.NextBlockHash.SequenceEqual(new byte[32]))
                .FirstOrDefault();

            if (highestBlock != null)
            {
                return highestBlock;
            }

            // if none, then we only have an unlinked genesis block
            if (highestBlock == null) // this only happens for the first block after the genesis block
                return Db.ValcoinBlocks.FirstOrDefault(b => b.BlockNumber == highestBlockNumber);

            // there's probably a fancy LINQ statement for this, but I couldn't get one to work
            foreach (var highBlock in Db.ValcoinBlocks.Where(b => b.BlockNumber == highestBlockNumber).ToList())
            {

                foreach (var b2 in Db.ValcoinBlocks.Where(b => b.BlockNumber == (highBlock.BlockNumber - 1)).ToList())
                {
                    if (b2.NextBlockHash.SequenceEqual(highBlock.BlockHash))
                    {
                        return highBlock;
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

        public async Task<List<ValcoinBlock>> GetAllBlocks()
        {
            return await Db.ValcoinBlocks.ToListAsync();
        }

        public async Task<Transaction> GetTx(string transactionId)
        {
            return await Db.Transactions.FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        /// <summary>
        /// This checks if a transaction has been spent already by finding a transaction with the same ID and index reference.
        /// </summary>
        /// <param name="previousTransactionId"></param>
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

        public async Task<List<Transaction>> GetAllMainChainTransactions()
        {
            var blocks = await GetAllBlocks();
            var txs = new List<Transaction>();
            foreach (var block in blocks)
            {
                if (Convert.ToHexString(block.NextBlockHash) == new string('0', 64))
                    continue; // not main chain

                block.Transactions.ForEach(t => txs.Add(t));
            }

            // the last main chain will always have a next of 00000... so we need to add it specially
            var highest = await GetLastMainChainBlock();
            if (highest != null)
                highest.Transactions.ForEach(t => txs.Add(t));

            return txs;
        }

        public async Task AddTxs(IEnumerable<Transaction> transactions)
        {
            foreach (Transaction tx in transactions)
            {
                Db.Add(tx);
            }
            await Db.SaveChangesAsync();
        }

        public async Task AddBlock(ValcoinBlock block, bool fromNetwork)
        {
            // remove any pending transactions of ours
            block.Transactions.ForEach(t => Db.PendingTransactions.Where(p => p.TransactionId == t.TransactionId)
                .ToList()
                .ForEach(p => Db.PendingTransactions.Remove(p)));
            await Db.SaveChangesAsync();

            // remove any transactions in our transaction pool that were in this block.
            // helps our next block we mine to not be immediately invalid
            block.Transactions.ForEach(t => MiningService.TransactionPool.Where(p => p.Key == t.TransactionId)
                .ToList()
                .ForEach(p => MiningService.TransactionPool.Remove(p.Key, out _)));

            var lastBlock = await GetLastMainChainBlock();

            if (lastBlock == null && block.BlockNumber == 1)
            {
                // this is the genesis block being added
                UpdateBalance(block);
                await CommitBlock(block);
            }
            else if (lastBlock == null)
            {
                // not the coinbase block, but something has happened. leave
                return;
            }
            // add a normal block to the chain. check that the lastBlock's number is one less than the incoming, and that the
            // new block references the last block
            else if (lastBlock.BlockNumber == block.BlockNumber - 1 && lastBlock.BlockHash.SequenceEqual(block.PreviousBlockHash))
            {
                UpdateBalance(block);
                // update the next newHighestBlock identifier
                block.BlockHash.CopyTo(lastBlock.NextBlockHash, 0);
                await UpdateBlock(lastBlock);
                await CommitBlock(block);
            }
            // check if this newHighestBlock is part of a longer blockchain
            else if (block.PreviousBlockHash != lastBlock.BlockHash && block.BlockNumber == lastBlock.BlockNumber + 1)
                await Reorganize(block, lastBlock);
            else
            {
                // this is some other block from the network, possibly an orphan or some other block we requested
                // reset the next hash as this may be an orphan chain. It should be re-added during the reorganization if needed.
                block.NextBlockHash = new byte[32];
                await CommitBlock(block);
            }

            if (fromNetwork)
                MiningService.NewBlockFoundFromNetwork = true;
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
            if (Db.ValcoinBlocks.FirstOrDefault(b => b.BlockId == block.BlockId) != null)
            {
                // we already got this block from where else, like a sync request or a getpreviousblock request
                return;
            }
            try
            {
                Db.Add(block);
                await Db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // nothing, this happens sometimes especially during debug. We already have the block, so there's nothing to save
            }
        }

        public async Task AddPendingTransaction(Transaction tx)
        {
            var block = await GetLastMainChainBlock();
            var blockNumber = block == null ? 0 : block.BlockNumber;
            var px = new PendingTransaction(tx.TransactionId, tx.Outputs.Sum(o => o.Amount), blockNumber);
            await CommitPendingTransaction(px);
        }

        public async Task<List<Transaction>> GetTransactionsAtOrAfterBlock(ulong blockNumber)
        {
            return await Db.Transactions.Where(t => t.BlockNumber >= blockNumber).ToListAsync();
        }

        public async Task CommitPendingTransaction(PendingTransaction ptx)
        {
            Db.Add(ptx);
            await Db.SaveChangesAsync();
        }

        public async Task UnloadPendingTransactions(ulong blockNumber, int pendingTransactionTimeout)
        {
            (await Db.PendingTransactions.ToListAsync())
                .Where(p => blockNumber - p.CurrentBlockNumber >= (ulong)pendingTransactionTimeout)
                .ToList()
                .ForEach(p => Db.PendingTransactions.Remove(p));
            await Db.SaveChangesAsync();
        }

        public virtual void UpdateBalance(ValcoinBlock block)
        {
            // add any payments we may have gotten
            block.Transactions
                .ForEach(t => t.Outputs
                    .Where(o => o.Address.SequenceEqual(myWallet.AddressBytes) == true)
                    .ToList()
                    .ForEach(async o => await AddToBalance(o.Amount, t.Outputs.IndexOf(o), t.TransactionId)));

            // subtract any payments we spent
            block.Transactions
                .Where(t => t.Inputs[0].PreviousTransactionId != new string('0', 64)) // filter out coinbase (this transaction is verified)
                .ToList()
                .ForEach(t => t.Inputs
                    .Where(i => i.UnlockerPublicKey.SequenceEqual(myWallet.PublicKey) == true)
                    .ToList()
                    .ForEach(async i => await SubtractFromBalance(i)));
        }

        public async Task AddWallet(Wallet wallet)
        {
            Db.Add(wallet);
            await Db.SaveChangesAsync();
        }

        public virtual async Task<Wallet> GetMyWallet()
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
            // return our unspent outputs minus our pending transactions
            return await Db.UTXOs.SumAsync(u => u.Amount) - await Db.PendingTransactions.SumAsync(p => p.Amount);
        }

        private async Task AddToBalance(int amount, int index, string txId)
        {
            var utxo = new UTXO(txId, index, amount);
            Db.Add(utxo);
            await Db.SaveChangesAsync();
        }

        private async Task SubtractFromBalance(TxInput input)
        {
            Db.UTXOs.Where(u => u.TransactionId == input.PreviousTransactionId)
                .Where(u => u.OutputIndex == input.PreviousOutputIndex)
                .ToList()
                .ForEach(u => Db.Remove(u));
            await Db.SaveChangesAsync();
        }

        private async Task SubtractFromBalance(UTXO utxo)
        {
            Db.Remove(utxo);
            await Db.SaveChangesAsync();
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
        private async Task Reorganize(ValcoinBlock newHighestBlock, ValcoinBlock lastBlock)
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

            // it's possible to enter this method multiple times, so we need to ensure we're locked
            if (!reorganizing)
            {
                lock (padlock)
                {
                    if (!reorganizing)
                    {
                        reorganizing = true;
                    }
                    else { return; }
                }
            }
            else { return; }


            if(lastBlock.BlockNumber !=  newHighestBlock.BlockNumber - 1)
            {
                throw new Exception("Last block number was not one less than new highest");
            }

            Stack<ValcoinBlock> newChain = new();
            Stack<ValcoinBlock> currentChain = new();
            bool differentGenesis = false;

            newChain.Push(await GetBlock(Convert.ToHexString(newHighestBlock.PreviousBlockHash))); // push the new block's previous hash, (eg, new is #48, prev is #47)
            currentChain.Push(await GetBlock(Convert.ToHexString(lastBlock.BlockHash)));           // and our current highest, so they are at the same height (current is #47)

            while (!differentGenesis && newChain.Peek().BlockId != currentChain.Peek().BlockId) // eventually we will converge on a single block (where the chain split).
            {
                // keep stacking down the chain, until we find the block that they split from (the branch block)
                newChain.Push(await GetBlock(Convert.ToHexString(newChain.Peek().PreviousBlockHash)));
                currentChain.Push(await GetBlock(Convert.ToHexString(currentChain.Peek().PreviousBlockHash)));

                //if (GetBlock(Convert.ToHexString(newChain.Peek().PreviousBlockHash)).Result.PreviousBlockHash.SequenceEqual(new byte[32]))
                if (GetBlock(Convert.ToHexString(newChain.Peek().PreviousBlockHash)).Result == null
                    && Convert.ToHexString(newChain.Peek().PreviousBlockHash) == new string('0', 64))
                {
                    // client may have a different genesis block. check ours as well
                    //if (GetBlock(Convert.ToHexString(currentChain.Peek().PreviousBlockHash)).Result.PreviousBlockHash.SequenceEqual(new byte[32]))
                    if (GetBlock(Convert.ToHexString(currentChain.Peek().PreviousBlockHash)).Result == null
                        && Convert.ToHexString(currentChain.Peek().PreviousBlockHash) == new string('0', 64))
                    {
                        // different genesis. The current top-of-stack of each Stack is the genesis for the respective chain
                        differentGenesis = true;
                        //// add our genesis blocks
                        //newChain.Push(await GetBlock(Convert.ToHexString(newChain.Peek().PreviousBlockHash)));
                        //currentChain.Push(await GetBlock(Convert.ToHexString(currentChain.Peek().PreviousBlockHash)));
                    }
                }
            }

            /*
             * because each computer on the network builds a block with a different block hash (due to using UTCNow.Ticks, never will be *exactly* the same),
             * it's possible to build up a longer "secondary chain" that is racing with the current main chain to become the new main chain.
             * 
             * Block:        3 4 5 6 7 8 9
             *                 ⌌-□-□-□-□   <- secondary chain
             * our chain -> -■-■-■-■-■-■-▣ <- current main
             *                            \   block being mined
             * 
             * if the current chain is unlucky and loses to a longer secondary chain, we need to progress potentially quite a ways to migrate to the new chain
             * 
             * Block:        2 3 4 5 6 7 8 9
             *                 ⌌-□-□-□-□-□-□ <- secondary chain (now new main)
             * our chain -> -■-■-■-■-■-■-▣   <- current main
             *   branch block /
             * 
             * we build each stack until each stack's last item is the branch block. We'll now migrate to the new chain
             */

            var txsToReRelease = new List<Transaction>();
            var processedTxs = new List<Transaction>();
            var coinbases = new List<Transaction>();

            // build up the current chain
            while (currentChain.Count > 0)
            {
                var processingBlock = currentChain.Pop();
                processingBlock.Transactions.Where(t => t.Inputs[0].PreviousTransactionId != new string('0', 64)) // skip the coinbase transactions
                    .ToList()
                    .ForEach(t => txsToReRelease.Add(t));

                // now add the coinbases to a separate list, as they will need to be removed
                processingBlock.Transactions.Where(t => t.Inputs[0].PreviousTransactionId == new string('0', 64))
                    .ToList()
                    .ForEach(t => coinbases.Add(t));

                processingBlock.NextBlockHash = new byte[32]; // unlink the block
                Db.Update(processingBlock);
            }

            // build up the new chain
            while (newChain.Count > 0)
            {
                var processingBlock = newChain.Pop();
                // the last element we remove won't have a block in the stack
                processingBlock.NextBlockHash = newChain.TryPeek(out var nextBlock) ? nextBlock.BlockHash : newHighestBlock.BlockHash;

                // transactions in this chain have already been processed, so we need to note them down
                processingBlock.Transactions.ForEach(t => processedTxs.Add(t));
                Db.Update(processingBlock);
            }

            // remove all processed transactions from our tx list
            processedTxs.ForEach(t => txsToReRelease
                    .Where(r => r.TransactionId == t.TransactionId)
                    .ToList()
                    .ForEach(x => txsToReRelease.Remove(x)));

            // remove any UTXOs we have
            foreach (var tx in txsToReRelease)
            {
                foreach (var output in tx.Outputs)
                {
                    if (output.Address.SequenceEqual(myWallet.AddressBytes))
                    {
                        var utxo = new UTXO(tx.TransactionId, tx.Outputs.IndexOf(output), output.Amount);
                        await SubtractFromBalance(utxo);
                    }
                }
            }

            // same with coinbases
            foreach (var tx in coinbases)
            {
                foreach (var output in tx.Outputs)
                {
                    if (output.Address.SequenceEqual(myWallet.AddressBytes))
                    {
                        var utxo = new UTXO(tx.TransactionId, tx.Outputs.IndexOf(output), output.Amount);
                        await SubtractFromBalance(utxo);
                    }
                }
            }

            // add the remaining transactions to the pool for the miner. There should be no duplicates, but just in case, check
            txsToReRelease.ForEach(t => MiningService.TransactionPool.ToList()
                .Where(p => p.Key != t.TransactionId)
                .ToList()
                .ForEach(r => MiningService.TransactionPool.TryAdd(r.Key, r.Value)));

            await Db.SaveChangesAsync();

            // finally, actually add the new highest block
            await CommitBlock(newHighestBlock);

            reorganizing = false;
            MiningService.NewBlockFoundFromNetwork = true;
        }

        public async Task Transact(string recipient, int amount)
        {
            List<TxInput> inputs = new();
            List<TxOutput> outputs = new()
            {
                // add the recipient output
                new(0, Convert.FromHexString(recipient[2..])) // set the amount to 0 initially
            };
            // find the transaction's we'll use. Pick the smallest first to keep the UTXO DB smaller
            foreach (var utxo in Db.UTXOs.OrderBy( u => u.Amount))
            {
                inputs.Add(new(utxo.TransactionId, utxo.OutputIndex, myWallet.PublicKey));
                outputs[0].Amount += utxo.Amount;

                // we will always need a list of inputs that is greater than or equal to the amount we went to send.
                // if the sum is greater, then the difference will be returned to us in a second output in the transaction
                // amount = 25,
                // 1 + 1 + 3 + 5 + 6 + 6 + 7 = 29
                // to recipient = 25
                // back to us = 4

                if (outputs[0].Amount >= amount)
                    break;
            }

            // add our change, if any
            var change = outputs[0].Amount - amount;
            if (change > 0)
            {
                // correct the original output's amount
                outputs[0].Amount = amount;
                // add our change
                outputs.Add(new(change, myWallet.AddressBytes));
            }

            var tx = new Transaction(inputs, outputs);
            myWallet.SignTransactionInputs(ref tx);

            await SendTransaction(tx);
        }

        /// <summary>
        /// Commit the transaction to the <see cref="MiningService.TransactionPool"/> (if active), and relay the
        /// transaction to the network.
        /// </summary>
        /// <param name="tx">The transaction to send.</param>
        public async Task SendTransaction(Transaction tx)
        {
            await AddPendingTransaction(tx);
            
            MiningService.TransactionPool.TryAdd(tx.TransactionId, tx);

            var msg = new Message(tx);
            msg.MessageType = MessageType.TransactionShare;
            await App.Current.Services.GetService<INetworkService>().RelayData(msg);
        }

        /// <summary>
        /// Gets the wealth of all addresses in the chain. Each address has the specified amount of Valcoin.
        /// </summary>
        /// <returns>Returns a dictionary of addresses and amounts.</returns>
        public async Task<Dictionary<string, int>> GetAllAddressWealth()
        {
            var wealth = new Dictionary<string, int>();

            var txs = await GetAllMainChainTransactions();
            foreach (var tx in txs)
            {
                // outputs first because they're easy, just add them up
                foreach (var output in tx.Outputs)
                {
                    var address = "0x" + Convert.ToHexString(output.Address);
                    if (!wealth.ContainsKey(address))
                    {
                        wealth.Add(address, output.Amount);
                    }
                    else
                    {
                        wealth[address] += output.Amount;
                    }
                }

                // inputs are slightly more complicated as we need to get the referenced transaction first,
                // find the owning address,then subtract the output amount from that address.
                foreach (var input in tx.Inputs)
                {
                    if (input.PreviousTransactionId == new string('0', 64))
                        continue; // this is a coinbase

                    var ptx = await Db.Transactions.FirstOrDefaultAsync(t => t.TransactionId == input.PreviousTransactionId);
                    var address = "0x" + Convert.ToHexString(ptx.Outputs[input.PreviousOutputIndex].Address);
                    var amount = ptx.Outputs.Sum(o => o.Amount);

                    if (!wealth.ContainsKey(address))
                    {
                        wealth.Add(address, 0 - amount);
                    }
                    else
                    {
                        wealth[address] -= amount;
                    }
                }
            }

            var orderedWealth = wealth.OrderBy(w => w.Value).ToDictionary(x => x.Key, x => x.Value);
            return orderedWealth;
        }
    }
}
