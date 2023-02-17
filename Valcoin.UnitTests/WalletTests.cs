using Microsoft.UI.Xaml.Documents;
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

        [Fact]
        public void SignsDataCorrectly()
        {
            var sigStruct = new UnlockSignatureStruct(1, wallet.PublicKey);

            var unlockSignature = wallet.SignData(sigStruct);

            var valid = Wallet.VerifyData(sigStruct, unlockSignature, wallet.PublicKey);
            Assert.True(valid);
        }

        [Fact]
        public void SignsDataCorrectlyInTransactions()
        {
            var coinbase = new Transaction(
                1,
                new TxInput[]
                {
                    new TxInput()
                    {
                        PreviousOutputIndex = -1,
                        PreviousTransactionId = new string('0', 64),
                        UnlockerPublicKey = wallet.PublicKey,
                        UnlockSignature = wallet.SignData(new UnlockSignatureStruct(1, wallet.PublicKey))
                    },
                },
                new TxOutput[]
                {
                    new TxOutput()
                    {
                        Amount = 50,
                        LockSignature = wallet.PublicKey
                    }
                }
            );

            var spend = new Transaction(
                2,
                new TxInput[]
                {
                    new TxInput()
                    {
                        PreviousOutputIndex = 0,
                        PreviousTransactionId = "795BED3A2BD915B304117228EEDC1F4E6091AB9B865D464A31DC1BEA9004A35A", // not real, random from db
                        UnlockerPublicKey = wallet.PublicKey,
                        UnlockSignature = wallet.SignData(new UnlockSignatureStruct(coinbase.BlockNumber, wallet.PublicKey))
                    },
                },
                new TxOutput[]
                {
                    new TxOutput()
                    {
                        Amount = 50,
                        LockSignature = wallet.PublicKey
                    }
                }
            );

            var valid = Wallet.VerifyData(
                new UnlockSignatureStruct(coinbase.BlockNumber, wallet.PublicKey),
                spend.Inputs[0].UnlockSignature,
                spend.Inputs[0].UnlockerPublicKey);
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
                TimeUTC = DateTime.Parse("2023-02-15T20:17:11.0000000-08:00")
            };

            var coinbase = new Transaction(
                block1.BlockNumber,
                new TxInput[]
                {
                    new TxInput()
                    {
                        PreviousOutputIndex = -1,
                        PreviousTransactionId = new string('0', 64),
                        UnlockerPublicKey = wallet.PublicKey,
                        UnlockSignature = wallet.SignData(new UnlockSignatureStruct(block1.BlockNumber, wallet.PublicKey))
                    },
                },
                new TxOutput[]
                {
                    new TxOutput()
                    {
                        Amount = 50,
                        LockSignature = wallet.PublicKey
                    }
                }
            );

            block1.AddTx(coinbase);
            block1.ComputeAndSetHash();

            var block2 = new ValcoinBlock
            {
                BlockNumber = 2,
                BlockDifficulty = 1,
                Nonce = 1111,
                TimeUTC = DateTime.Parse("2023-02-15T20:18:11.0000000-08:00")
            };

            var coinbase2 = new Transaction(
                block2.BlockNumber,
                new TxInput[]
                {
                    new TxInput()
                    {
                        PreviousOutputIndex = -1,
                        PreviousTransactionId = new string('0', 64),
                        UnlockerPublicKey = wallet.PublicKey,
                        UnlockSignature = wallet.SignData(new UnlockSignatureStruct(block2.BlockNumber, wallet.PublicKey))
                    },
                },
                new TxOutput[]
                {
                    new TxOutput()
                    {
                        Amount = 50,
                        LockSignature = wallet.PublicKey
                    }
                }
            );

            var spend = new Transaction(
                block2.BlockNumber,
                new TxInput[]
                {
                    new TxInput()
                    {
                        PreviousOutputIndex = 0,
                        PreviousTransactionId = block1.Transactions[0].TxId,
                        UnlockerPublicKey = wallet.PublicKey,
                        UnlockSignature = wallet.SignData(new UnlockSignatureStruct(block1.BlockNumber, wallet.PublicKey))
                    },
                },
                new TxOutput[]
                {
                    new TxOutput()
                    {
                        Amount = 50,
                        LockSignature = wallet.PublicKey
                    }
                }
            );

            block2.AddTx(coinbase2);
            block2.AddTx(spend);

            var valid = Wallet.VerifyData(
                new UnlockSignatureStruct(block1.BlockNumber, wallet.PublicKey),
                block2.Transactions[1].Inputs[0].UnlockSignature,
                block2.Transactions[1].Inputs[0].UnlockerPublicKey);
            Assert.True(valid);
        }

        [Fact]
        public void VerifiesDataCorrectlyWithImportedPublicKey()
        {
            // string exported from the wallet params above
            var publicKey = Convert.FromHexString("3059301306072A8648CE3D020106082A8648CE3D03010703420004BED612DDD11CA8237AF64DEE0EF9B5605A7C487C97E457F117D23CD111BFB376DA9EF038E8A08898A219171226107ACCB77DA940EA40B5CF0295BF28B7A2C5F0");
            var dataToSign = new UnlockSignatureStruct(1, publicKey);
            var signature = Convert.FromHexString("39599C71E871C7A5C07B098E8EAE3F9EC56E9ACCDF936B09843415298587A1F4BDCFB4EC3F40828C4E06EA90C656996FDE5047B85030ADC2DD94B2C43FAA5F56");

            var valid = Wallet.VerifyData(dataToSign, signature, publicKey);

            Assert.True(valid);
        }

        [Fact]
        public void VerifiesSerializedDataCorrectly()
        {
            var block1 = new ValcoinBlock
            {
                BlockNumber = 1,
                BlockDifficulty = 1,
                Nonce = 1111,
                TimeUTC = DateTime.Parse("2023-02-15T20:17:11.0000000-08:00")
            };

            var coinbase = new Transaction(
                block1.BlockNumber,
                new TxInput[]
                {
                    new TxInput()
                    {
                        PreviousOutputIndex = -1,
                        PreviousTransactionId = new string('0', 64),
                        UnlockerPublicKey = wallet.PublicKey,
                        UnlockSignature = wallet.SignData(new UnlockSignatureStruct(block1.BlockNumber, wallet.PublicKey))
                    },
                },
                new TxOutput[]
                {
                    new TxOutput()
                    {
                        Amount = 50,
                        LockSignature = wallet.PublicKey
                    }
                }
            );

            block1.AddTx(coinbase);
            block1.ComputeAndSetHash();

            var block1AsBytes = (byte[])block1;
            var block1d = JsonDocument.Parse(block1AsBytes).Deserialize<ValcoinBlock>();

            var dataToSign = new UnlockSignatureStruct(block1d.BlockNumber, block1d.Transactions[0].Inputs[0].UnlockerPublicKey);
            //var signedData = wallet.SignData(dataToSign);

            var valid = Wallet.VerifyData(dataToSign, block1d.Transactions[0].Inputs[0].UnlockSignature, block1d.Transactions[0].Inputs[0].UnlockerPublicKey);
            Assert.True(valid);
        }
    }
}
