using System.Security.Cryptography;
using Valcoin.Models;

namespace Valcoin.UnitTests
{
    public class TransactionTests
    {
        [Fact]
        public void BuildTransaction()
        {
            // build the coinbase transaction
            var wallet = new Wallet();
            wallet.Initialize();

            var input = new TxInput
            {
                PreviousTransactionId = "0000000000000000000000000000000000000000000000000000000000000000", // coinbase
                PreviousOutputIndex = -1, // FF FF FF FF
                UnlockerPublicKey = wallet.PublicKey,
                UnlockSignature = wallet.SignData(wallet.PublicKey)
            };

            var tx = new Transaction(50, wallet.GetAddressAsString(), new TxInput[] { input });
        }
    }
}