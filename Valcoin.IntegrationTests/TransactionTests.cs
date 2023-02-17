using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.IntegrationTests
{
    public class TransactionTests
    {
        readonly Wallet wallet;

        public TransactionTests()
        {
            // Create the test wallet with static data
            // this is a random public/private keyset that was generated. Not in use.
            ECParameters ecParams = new()
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = Convert.FromHexString("490CC7A8A8FDC8E2196591B86AECFA6B138968E7D828906E80715A379044932F"),
                Q = new ECPoint()
                {
                    X = Convert.FromHexString("BED612DDD11CA8237AF64DEE0EF9B5605A7C487C97E457F117D23CD111BFB376"),
                    Y = Convert.FromHexString("DA9EF038E8A08898A219171226107ACCB77DA940EA40B5CF0295BF28B7A2C5F0")
                }
            };
            var ecdsa = ECDsa.Create(ecParams);
            wallet = new Wallet(ecdsa.ExportSubjectPublicKeyInfo(), ecdsa.ExportECPrivateKey());
        }

        [Fact]
        public void BuildTransaction()
        {
            ulong blockId = 10; // this tx is part of block 10

            var input = new TxInput(new string('0', 64), -1, wallet.PublicKey, wallet.SignData(new UnlockSignatureStruct(blockId, wallet.PublicKey)));

            var output = new TxOutput("0", 50, wallet.AddressBytes);

            var tx = new Transaction(blockId, new List<TxInput> { input }, new List<TxOutput> { output });

            // assert on field that are generated and not statically assigned in the test
            Assert.NotNull(tx.TransactionId);
            Assert.NotNull(tx.Inputs);
            Assert.NotNull(tx.Outputs);
            Assert.NotNull(tx.JsonInputs);
            Assert.NotNull(tx.JsonOutputs);
            Assert.NotNull(tx.Inputs[0].UnlockerPublicKey);
            Assert.NotNull(tx.Inputs[0].UnlockSignature);
            Assert.NotNull(tx.Outputs[0].LockSignature);
        }
    }
}
