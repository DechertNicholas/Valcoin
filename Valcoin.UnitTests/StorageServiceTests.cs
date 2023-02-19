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
    }
}
