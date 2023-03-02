using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public async void ParsesOneMessage()
        {
            // token to cleanly cancel our tasks
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            var chainMock = new Mock<IChainService>();
            var miningMock = new Mock<IMiningService>();

            chainMock.Setup(s => s.GetClients()).ReturnsAsync(new List<Client>());
            chainMock.Setup(s => s.GetBlock(It.IsAny<string>())); // returns null
            
            var networkService = new NetworkService(chainMock.Object, miningMock.Object);

            var blockId = "123";
            var message = new Message(blockId); // block request message with fake id
            var client = new Client(IPAddress.Broadcast.ToString(), 2106); // send data to ourselves

            // we need to run this in the background, use a task
            await Task.Run(() => networkService.StartListener(token), token);
            await networkService.SendData(message, client);
            Thread.Sleep(1000); // sleep 1 second to let the service get setup

            // let the service parse the message we sent
            var parsing = true;
            while (parsing)
            {
                foreach(var task in NetworkService.ActiveParses)
                {
                    if (task.IsCompleted != true)
                        break;
                    parsing = false;
                }

            }
            tokenSource.Cancel();

            chainMock.Verify(m => m.GetClients(), Times.Exactly(3));
            chainMock.Verify(m => m.GetBlock(It.IsAny<string>()), Times.Once);
            chainMock.Verify(m => m.AddClient(It.IsAny<Client>()), Times.Once);
            chainMock.Verify(m => m.GetLastMainChainBlock(), Times.Exactly(2));
            chainMock.Verify(m => m.GetBlocksByNumber(0), Times.Once);
            chainMock.Verify(m => m.UpdateClient(It.IsAny<Client>()), Times.Exactly(2));

            chainMock.VerifyNoOtherCalls();
            miningMock.VerifyNoOtherCalls();
        }
    }
}
