using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.UnitTests.SharedData
{
    public static class ValidationServiceShared
    {
        public static ValcoinBlock ValidCoinbaseOnlyBlock { get; }
        public static ValcoinBlock ValidSpendBlock { get; }

        static ValidationServiceShared()
        {
            var wallet = MakeTestingWallet();

            var block1 = new ValcoinBlock
            {
                BlockNumber = 1,
                BlockDifficulty = 1,
                Nonce = 1111,
                TimeUTCTicks = DateTime.Parse("2023-02-15T20:17:11.0000000-08:00").Ticks
            };

            var coinbase = new Transaction(
                block1.BlockNumber,
                new List<TxInput>()
                {
                    new TxInput(new string('0', 64), -1, wallet.PublicKey)
                },
                new List<TxOutput>()
                {
                    new TxOutput(50, wallet.AddressBytes)
                }
            );
            wallet.SignTransactionInputs(ref coinbase);

            block1.AddTx(coinbase);
            block1.ComputeAndSetHash();

            var block2 = new ValcoinBlock
            {
                BlockNumber = 2,
                BlockDifficulty = 1,
                Nonce = 1111,
                TimeUTCTicks = DateTime.Parse("2023-02-15T20:18:11.0000000-08:00").Ticks
            };

            var coinbase2 = new Transaction(
                block2.BlockNumber,
                new List<TxInput>()
                {
                    new TxInput(new string('0', 64), -1, wallet.PublicKey)
                },
                new List<TxOutput>()
                {
                    new TxOutput(50, wallet.AddressBytes)
                }
            );
            wallet.SignTransactionInputs(ref coinbase2);

            var spend = new Transaction(
                block2.BlockNumber,
                new List<TxInput>()
                {
                    new TxInput(block1.Transactions[0].TransactionId, 0, wallet.PublicKey)
                },
                new List<TxOutput>()
                {
                    new TxOutput(50, wallet.AddressBytes)
                }
            );
            wallet.SignTransactionInputs(ref spend);

            block2.AddTx(coinbase2);
            block2.AddTx(spend);
            block2.ComputeAndSetHash();

            ValidCoinbaseOnlyBlock = block1;
            ValidSpendBlock = block2;
        }

        private static Wallet MakeTestingWallet()
        {
            // random wallet that was generated
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
            return new Wallet(ecdsa.ExportSubjectPublicKeyInfo(), ecdsa.ExportECPrivateKey());
        }
    }
}
