using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.IntegrationTests
{
    [Collection(nameof(DatabaseCollection))]
    public class ValcoinBlockTestsDBRequired
    {
        readonly DatabaseFixture fixture;

        public ValcoinBlockTestsDBRequired(DatabaseFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void VerifyReadWriteToDB()
        {
            TxInput input;
            TxOutput output;
            ValcoinBlock block;

            block = new(0, new byte[32], 0, DateTime.UtcNow, 22);

            input = new() // coinbase
            {
                PreviousTransactionId = new string('0', 64),
                PreviousOutputIndex = 0,
                UnlockerPublicKey = fixture.Wallet.PublicKey,
                UnlockSignature = fixture.Wallet.SignData(new UnlockSignatureStruct { BlockId = block.BlockId, PublicKey = fixture.Wallet.PublicKey })
            };

            output = new()
            {
                Amount = 50,
                LockSignature = fixture.Wallet.AddressBytes
            };

            var txs = new List<Transaction>();

            // add multiple transactions
            for (var i = 0; i < 5; i++)
            {
                txs.Add(new Transaction(block.BlockId, new TxInput[] { input }, new TxOutput[] { output }));
            }

            block.AddTx(txs);

            block.ComputeAndSetHash();
            fixture.Context.Add(block);
            fixture.Context.SaveChanges();

            var verify = fixture.Context.ValcoinBlocks.FirstOrDefault(b => b.BlockId == block.BlockId);
            Assert.NotNull(verify);
            if (null != verify)
            {
                Assert.Equal(verify.BlockId, block.BlockId);
            }
        }
    }
}
