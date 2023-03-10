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
        public void VerifyReadWriteToDB()
        {
            ValcoinBlock block = new(1, new byte[32], 0, DateTime.UtcNow.Ticks, 22);

            var tx1Inputs = new List<TxInput>()
            { 
                new TxInput(new string('0', 64), -1, fixture.Wallet.PublicKey)
            };
            var tx1Outputs = new List<TxOutput>() { new TxOutput(50, fixture.Wallet.AddressBytes) };
            var tx1 = new Transaction(block.BlockNumber, tx1Inputs, tx1Outputs);
            fixture.Wallet.SignTransactionInputs(ref tx1);

            var tx2Inputs = new List<TxInput>()
            { 
                new TxInput(new string('2', 64), -1, fixture.Wallet.PublicKey)
            };
            // assign two outputs
            var tx2Outputs = new List<TxOutput>() { new TxOutput(50, fixture.Wallet.AddressBytes), new TxOutput(30, fixture.Wallet.AddressBytes) };
            var tx2 = new Transaction(block.BlockNumber, tx2Inputs, tx2Outputs);
            fixture.Wallet.SignTransactionInputs(ref tx2);


            block.AddTx(tx1);
            block.AddTx(tx2);

            block.ComputeAndSetHash();
            fixture.Context.Add(block);
            fixture.Context.SaveChanges();

            var verify = fixture.Context.ValcoinBlocks.First(b => b.BlockId == block.BlockId);
            verify.ComputeAndSetMerkleRoot();
            verify.ComputeAndSetHash();
            Assert.True(block.MerkleRoot.SequenceEqual(verify.MerkleRoot));

            // assert equal after re-computing the block's hash
            var hashEq = block.BlockHash.SequenceEqual(verify.BlockHash);
            Assert.True(hashEq);
        }
    }
}
