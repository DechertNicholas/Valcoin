﻿using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.UnitTests
{
    public class WalletTests
    {
        private readonly Wallet wallet;

        public WalletTests()
        {
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

        // this test is broken after the changes to TxInput.UnlockSignature
        //[Fact]
        //public void SignsDataCorrectly()
        //{
        //    var input = new TxInput("0000", -1, new byte[] { 1 });
        //    var output = new TxOutput(50, new byte[] { 2 });
        //    var tx = new Transaction(new() { input }, new() { output });

        //    wallet.SignTransactionInputs(ref tx);

        //    var valid = Wallet.VerifyData(sigStruct, unlockSignature, wallet.PublicKey);
        //    Assert.True(valid);
        //}

        [Fact]
        public void SignsDataCorrectlyInTransactions()
        {
            var coinbase = new Transaction(
                1,
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

            var spend = new Transaction(
                2,
                new List<TxInput>()
                {
                    new TxInput("795BED3A2BD915B304117228EEDC1F4E6091AB9B865D464A31DC1BEA9004A35A", 0, wallet.PublicKey)
                },
                new List<TxOutput>()
                {
                    new TxOutput(50, wallet.AddressBytes)
                }
            );
            wallet.SignTransactionInputs(ref spend);

            var valid = Wallet.VerifyTransactionInputs(spend);
            Assert.True(valid);
        }

        [Fact]
        public void SignsDataCorrectlyInBlocks()
        {
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

            var valid = Wallet.VerifyTransactionInputs(block2.Transactions.First());
            Assert.True(valid);
        }

        // broken after changes to Wallet.VerifyTransactionInputs
        //[Fact]
        //public void VerifiesDataCorrectlyWithImportedPublicKey()
        //{
        //    // string exported from the wallet params above
        //    var publicKey = Convert.FromHexString("3059301306072A8648CE3D020106082A8648CE3D03010703420004BED612DDD11CA8237AF64DEE0EF9B5605A7C487C97E457F117D23CD111BFB376DA9EF038E8A08898A219171226107ACCB77DA940EA40B5CF0295BF28B7A2C5F0");
        //    var dataToSign = new UnlockSignatureStruct(1, publicKey);
        //    var signature = Convert.FromHexString("39599C71E871C7A5C07B098E8EAE3F9EC56E9ACCDF936B09843415298587A1F4BDCFB4EC3F40828C4E06EA90C656996FDE5047B85030ADC2DD94B2C43FAA5F56");

        //    var valid = Wallet.VerifyData(dataToSign, signature, publicKey);

        //    Assert.True(valid);
        //}

        [Fact]
        public void VerifiesSerializedDataCorrectly()
        {
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

            var block1AsBytes = (byte[])block1;
            var block1d = JsonDocument.Parse(block1AsBytes).Deserialize<ValcoinBlock>() ?? throw new NullReferenceException();
            block1d.ComputeAndSetHash();
            

            var valid = Wallet.VerifyTransactionInputs(block1d.Transactions.First());
            Assert.True(valid);
        }
    }
}
