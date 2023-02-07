using System.Security.Cryptography;
using Valcoin.Models;

namespace Valcoin.UnitTests
{
    public class TransactionTests
    {
        [Fact]
        public void BuildTransaction()
        {
            //var test = "2D6F7AD05EC21F715ECD0CEA77DC95A19D21F7F1A01B806FB52EB3FE48CCF963";
            //var test2 = Convert.FromHexString(test);
            var bitcoinTx = "0100000001c997a5e56e104102fa209c6a852dd90660a20b2d9c352423edce25857fcd3704000000004847304402204e45e16932b8af514961a1d3a1a25fdf3f4f7732e9d624c6c61548ab5fb8cd410220181522ec8eca07de4860a4acdd12909d831cc56cbbac4622082221a8768d1d0901ffffffff0200ca9a3b00000000434104ae1a62fe09c5f51b13905f07f06b99a2f7159b2225f374cd378d71302fa28414e7aab37397f554a7df5f142c21c1b7303b8a0626f1baded5c72a704f7e6cd84cac00286bee0000000043410411db93e1dcdb8a016b49840f8c53bc1eb68a382e97b1482ecad7b148a6909a5cb2e0eaddfb84ccf9744464f82e160bfa9b8b64f9d4c03f999b8643f656b412a3ac00000000";
            var bytes = Convert.FromHexString(bitcoinTx);
            var hash1 = SHA256.Create().ComputeHash(bytes);
            var hash2 = SHA256.Create().ComputeHash(hash1);

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