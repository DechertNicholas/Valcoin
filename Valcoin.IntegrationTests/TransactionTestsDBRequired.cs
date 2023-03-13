using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;

namespace Valcoin.IntegrationTests
{
    [Collection(nameof(DatabaseCollection))]
    public class TransactionTestsDBRequired
    {
        readonly DatabaseFixture fixture;

        public TransactionTestsDBRequired(DatabaseFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async void VerifyDataReadWriteToDB()
        {
            long blockId = 10; // this tx is part of block 10

            var input = new 
                TxInput(new string('0', 64), -1, fixture.Wallet.PublicKey);

            var output = new TxOutput(50, fixture.Wallet.AddressBytes);

            var tx = new Transaction(blockId, new List<TxInput> { input }, new List<TxOutput> { output });
            fixture.Wallet.SignTransactionInputs(ref tx);

            fixture.Context.Add(tx);
            await fixture.Context.SaveChangesAsync();

            var txVerify = fixture.Context.Transactions.FirstOrDefault(t => t.TransactionId == tx.TransactionId);
            Assert.NotNull(txVerify);
        }
    }
}
