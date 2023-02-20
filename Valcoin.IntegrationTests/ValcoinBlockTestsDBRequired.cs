using Microsoft.EntityFrameworkCore;
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
            ValcoinBlock block = new(1, new byte[32], 0, DateTime.UtcNow.Ticks, 22);

            var tx1Inputs = new List<TxInput>() { new TxInput(new string('0', 64), -1, fixture.Wallet.PublicKey,
                fixture.Wallet.SignData(new UnlockSignatureStruct(block.BlockNumber, fixture.Wallet.PublicKey)))};
            var tx1Outputs = new List<TxOutput>() { new TxOutput(50, fixture.Wallet.AddressBytes) };
            var tx1 = new Transaction(block.BlockNumber, tx1Inputs, tx1Outputs);

            var tx2Inputs = new List<TxInput>() { new TxInput(new string('2', 64), -1, fixture.Wallet.PublicKey,
                fixture.Wallet.SignData(new UnlockSignatureStruct(block.BlockNumber, fixture.Wallet.PublicKey)))};
            // assign two outputs
            var tx2Outputs = new List<TxOutput>() { new TxOutput(50, fixture.Wallet.AddressBytes), new TxOutput(30, fixture.Wallet.AddressBytes) };
            var tx2 = new Transaction(block.BlockNumber, tx2Inputs, tx2Outputs);


            block.AddTx(tx1);
            block.AddTx(tx2);

            block.ComputeAndSetHash();
            fixture.Context.Add(block);
            await fixture.Context.SaveChangesAsync();

            // dispose of the old fixture because issues were identified between loading models between one fixture and another
            fixture.Dispose();
            DatabaseFixture fixture2 = new();

            var verify = fixture2.Context.ValcoinBlocks.FirstOrDefault(b => b.BlockNumber == block.BlockNumber);
            verify.ComputeAndSetMerkleRoot();
            verify.ComputeAndSetHash();
            Assert.True(block.MerkleRoot
                .SequenceEqual(verify.MerkleRoot));

            // assert equal after re-computing the block's hash
            Assert.True(block.BlockHash
                .SequenceEqual(verify.BlockHash));
        }
    }
}
