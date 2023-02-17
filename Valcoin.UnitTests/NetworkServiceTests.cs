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
            var wallet = Wallet.Create();
            ulong blockId = 10; // this tx is part of block 10

            var input = new TxInput(new string('0', 64), -1, wallet.PublicKey,
                wallet.SignData(new UnlockSignatureStruct(blockId, wallet.PublicKey)));

            var output = new TxOutput("0", 50, wallet.AddressBytes);

            var tx = new Transaction(blockId, new List<TxInput> { input }, new List<TxOutput> { output });

            var t = Task.Run(() => NetworkService.StartListener());
            //await NetworkService.SendData(new byte[] {0,1,2,3,4,5,6,7,8,9});
            //await NetworkService.SendData(tx);

            //t.Wait();
        }
    }
}
