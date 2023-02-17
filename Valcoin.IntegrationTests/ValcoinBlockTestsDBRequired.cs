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
        public async void VerifyReadWriteToDB()
        {
            ValcoinBlock block;

            block = new(1, new byte[32], 0, DateTime.UtcNow, 22);

            var tx1Inputs = new List<TxInput>() { new TxInput(new string('0', 64), -1, fixture.Wallet.PublicKey,
                fixture.Wallet.SignData(new UnlockSignatureStruct(block.BlockNumber, fixture.Wallet.PublicKey)))};
            var tx1Outputs = new List<TxOutput>() { new TxOutput("0", 50, fixture.Wallet.AddressBytes) };
            var tx1 = new Transaction(block.BlockNumber, tx1Inputs, tx1Outputs);

            var tx2Inputs = new List<TxInput>() { new TxInput(new string('1', 64), -1, fixture.Wallet.PublicKey,
                fixture.Wallet.SignData(new UnlockSignatureStruct(block.BlockNumber, fixture.Wallet.PublicKey)))};
            var tx2Outputs = new List<TxOutput>() { new TxOutput("0", 50, fixture.Wallet.AddressBytes) };
            var tx2 = new Transaction(block.BlockNumber, tx2Inputs, tx2Outputs);


            block.AddTx(tx1);
            block.AddTx(tx2);

            block.ComputeAndSetHash();
            fixture.Context.Add(block);
            await fixture.Context.SaveChangesAsync();

            var verify = fixture.Context.ValcoinBlocks.FirstOrDefault(b => b.BlockNumber == block.BlockNumber);
            var h = SHA256.Create();
            Assert.True(h.ComputeHash(block)
                .SequenceEqual(h.ComputeHash(verify)));

            // assert equal after re-computing the block's hash
            verify.ComputeAndSetHash();
            Assert.True(h.ComputeHash(block)
                .SequenceEqual(h.ComputeHash(verify)));
        }
    }
}
