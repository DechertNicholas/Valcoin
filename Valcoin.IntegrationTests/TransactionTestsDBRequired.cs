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
        public void VerifyDataReadWriteToDB()
        {
            ulong blockId = 10; // this tx is part of block 10

            var input = new TxInput
            {
                PreviousTransactionId = new string('0', 64), // coinbase
                PreviousOutputIndex = -1, // 0xffffffff
                UnlockerPublicKey = fixture.Wallet.PublicKey, // this doesn't matter for the coinbase transaction
                UnlockSignature = fixture.Wallet.SignData(new UnlockSignatureStruct(blockId, fixture.Wallet.PublicKey)) // neither does this
            };

            var output = new TxOutput
            {
                Amount = 50,
                LockSignature = fixture.Wallet.AddressBytes // this does though, as no one should spend these coins other than the owner
                                                    // of this hashed public key
            };

            var tx = new Transaction(blockId, new TxInput[] { input }, new TxOutput[] { output });

            fixture.Context.Add(tx);
            fixture.Context.SaveChanges();

            var txVerify = fixture.Context.Transactions.FirstOrDefault(t => t.TxId == tx.TxId);
            Assert.NotNull(txVerify);
        }
    }
}
