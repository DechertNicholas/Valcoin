using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;

namespace Valcoin.UnitTests
{
    public class StorageServiceTests
    {
        [Fact]
        public async void ReturnsPreviouslyReferencedTransaction()
        {
            // this coinbase transaction creates 50 new Valcoins
            var coinbase = new Transaction(
                1,
                new List<TxInput>()
                {
                    new TxInput(new string('0', 64), -1, new byte[] { 1 },
                        new byte[] { 2 })
                },
                new List<TxOutput>()
                {
                    new TxOutput(50, new byte[] { 3 })
                }
            );

            // this transaction spends those coins
            var spend = new Transaction(
                2,
                new List<TxInput>()
                {
                    new TxInput(coinbase.TransactionId, 0, new byte[] { 1 },
                        new byte[] { 2 })
                },
                new List<TxOutput>()
                {
                    new TxOutput(50, new byte[] { 3 })
                }
            );

            // this transaction attempts to spend them again
            var doubleSpend = new Transaction(
                3,
                new List<TxInput>()
                {
                    new TxInput(coinbase.TransactionId, 0, new byte[] { 1 },
                        new byte[] { 2 })
                },
                new List<TxOutput>()
                {
                    new TxOutput(50, new byte[] { 3 })
                }
            );

            var txs = new List<Transaction>() { coinbase, spend };

            var service = new StorageService();
            await service.AddTxs(txs);

            var result = await service.GetTxByInput(doubleSpend.Inputs[0].PreviousTransactionId, doubleSpend.Inputs[0].PreviousOutputIndex);
            Assert.Equal(spend.TransactionId, result.TransactionId);
        }

        [Fact]
        public async void ReturnsMainChainBlockWhenOrphanExists()
        {
            var service = new StorageService();

            var genesis = new ValcoinBlock(1, new byte[32], 11, DateTime.UtcNow.Ticks, 1);
            genesis.ComputeAndSetMerkleRoot();
            genesis.ComputeAndSetHash();

            await service.AddBlock(genesis);

            var block2 = new ValcoinBlock(2, genesis.BlockHash, 123, DateTime.UtcNow.Ticks, 1);
            block2.ComputeAndSetMerkleRoot();
            block2.ComputeAndSetHash();

            await service.AddBlock(block2);

            var orphan = new ValcoinBlock(2, genesis.BlockHash, 222, DateTime.UtcNow.Ticks, 1);
            orphan.ComputeAndSetMerkleRoot();
            orphan.ComputeAndSetHash();

            await service.AddBlock(orphan);

            var lastBlock = await service.GetLastBlock();

            Assert.True(block2.BlockHash.SequenceEqual(lastBlock.BlockHash));
        }
    }
}
