using Microsoft.Extensions.DependencyInjection;
using System;
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
        private byte[] myAddress;
        private byte[] myPublicKey;
        private readonly IStorageService storageService;
        private readonly IMiningService miningService;

        public ChainService(IStorageService storageService, IMiningService miningService)
        {
            this.storageService = storageService;
            this.miningService = miningService;
        }

        #region ProxyMethods
        // these are just proxy methods for the storage service to ensure that all chain-related operations can be performed through the chain service

        public async Task<ValcoinBlock> GetLastMainChainBlock()
        {
            return await storageService.GetLastMainChainBlock();
        }

        public async Task<ValcoinBlock> GetBlock(string blockId)
        {
            return await storageService.GetBlock(blockId);
        }

        public async Task<Transaction> GetTx(string transactionId)
        {
            return await storageService.GetTx(transactionId);
        }

        public async Task<Transaction> GetTxByInput(string previousTransactionId, int outputIndex)
        {
            return await storageService.GetTxByInput(previousTransactionId, outputIndex);
        }

        #endregion

        public async Task AddBlock(ValcoinBlock block)
        {
            var lastBlock = await storageService.GetLastMainChainBlock();

            if (lastBlock == null)
            {
                // this is the genesis block being added
                await storageService.AddBlock(block);
                return;
            }

            // check if this newHighestBlock is part of a longer blockchain
            if (block.BlockNumber > (lastBlock.BlockNumber + 1))
                await Reorganize(block);

            // add a normal block to the chain. check that the lastBlock's number is one less than the incoming, and that the
            // new block references the last block
            else if (lastBlock.BlockNumber == block.BlockNumber - 1 && lastBlock.BlockHash.SequenceEqual(block.BlockHash))
            {
                await UpdateBalance(block);
                // update the next newHighestBlock identifier
                block.BlockHash.CopyTo(lastBlock.NextBlockHash, 0);
                await storageService.UpdateBlock(lastBlock);
                await storageService.AddBlock(block);
            }
            else
            {
                // this is some other block from the network, possibly an orphan or some other block we requested
                await storageService.AddBlock(block);
            }
        }

        private async Task UpdateBalance(ValcoinBlock block)
        {
            myAddress ??= (await storageService.GetMyWallet()).AddressBytes;
            myPublicKey ??= (await storageService.GetMyWallet()).PublicKey;
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

        private async Task AddToBalance(int payment)
        {
            var wallet = await storageService.GetMyWallet();
            wallet.Balance += payment;
            await storageService.UpdateWallet(wallet);
        }

        private async Task SubtractFromBalance(int payment)
        {
            var wallet = await storageService.GetMyWallet();
            wallet.Balance -= payment;
            await storageService.UpdateWallet(wallet);
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

            // stop the miner, if active
            var previousMinerStatus = miningService.MineBlocks;
            miningService.MineBlocks = false;
            miningService.Status = "Reorganizing Chain";

            var previousOrphan = await storageService.GetBlock(Convert.ToHexString(newHighestBlock.PreviousBlockHash));
            // the branch block is the block which had two different block referring back to it (the main chain and the orphan chain)
            var branchBlock = await storageService.GetBlock(Convert.ToHexString(previousOrphan.PreviousBlockHash));
            // the new orphan is the block that was previously in the main chain, that we are now disconnecting
            var newOrphan = await storageService.GetBlock(Convert.ToHexString(branchBlock.NextBlockHash));
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

            // add the remaining transactions to the pool for the miner. There should be no duplicates, but just in case, check
            txsToReRelease.ForEach(t => miningService.TransactionPool
                .Where(p => p.TransactionId != t.TransactionId)
                .ToList()
                .ForEach(r => miningService.TransactionPool.Add(r)));

            // update our branch block
            await storageService.UpdateBlock(branchBlock);

            // the previous orphan was already in the database, so we only need to update the NextBlockHash property
            previousOrphan.NextBlockHash = newHighestBlock.BlockHash;
            await storageService.UpdateBlock(previousOrphan);
            // the new orphan block is now an orphan (because branchBlock does not point to it) and there is no need
            // to perform any operations on it (other than having gotten the list of transactions).

            // now add the newHighestBlock
            await storageService.AddBlock(newHighestBlock);

            // restart the miner if it was active
            if (previousMinerStatus)
            {
                miningService.MineBlocks = previousMinerStatus;
                miningService.Status = "Mining";
            }
        }
    }
}
