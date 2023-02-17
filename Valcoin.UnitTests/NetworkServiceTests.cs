using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;

namespace Valcoin.UnitTests
{
    public class NetworkServiceTests
    {
        [Fact]
        public void SendData()
        {
            var wallet = new Wallet();
            ulong blockId = 10; // this tx is part of block 10

            var input = new TxInput
            {
                PreviousTransactionId = new string('0', 64), // coinbase
                PreviousOutputIndex = -1, // 0xffffffff
                UnlockerPublicKey = wallet.PublicKey, // this doesn't matter for the coinbase transaction
                UnlockSignature = wallet.SignData(new UnlockSignatureStruct(blockId, wallet.PublicKey)) // neither does this
            };

            var output = new TxOutput
            {
                Amount = 50,
                LockSignature = wallet.AddressBytes // this does though, as no one should spend these coins other than the owner
                                                    // of this hashed public key
            };

            var tx = new Transaction(blockId, new TxInput[] { input }, new TxOutput[] { output });

            var t = Task.Run(() => NetworkService.StartListener());
            //await NetworkService.SendData(new byte[] {0,1,2,3,4,5,6,7,8,9});
            //await NetworkService.SendData(tx);

            //t.Wait();
        }
    }
}
