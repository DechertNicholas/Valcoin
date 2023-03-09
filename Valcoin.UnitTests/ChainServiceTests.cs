using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq;
using Valcoin.Models;
using Valcoin.Services;
using Valcoin.UnitTests.SharedData;

namespace Valcoin.UnitTests
{
    [Collection(nameof(DatabaseCollection))]
    public class ChainServiceTests
    {
        readonly DatabaseFixture fixture;
        private readonly Mock<IMiningService> miningMock;

        public ChainServiceTests(DatabaseFixture fixture)
        {
            miningMock = new Mock<IMiningService>();
            this.fixture = fixture;
        }

        private static ValcoinBlock GetExampleBlock()
        {
            return new ValcoinBlock(1, new byte[] { 1 }, 123, DateTime.UtcNow.Ticks, 1)
            {
                Transactions = new() { GetExampleTransaction() }
            };
        }

        private static Transaction GetExampleTransaction()
        {
            return new Transaction(
                1,
                new() { new TxInput("0000", 0, new byte[] { 1 }) },
                new() { new TxOutput(50, new byte[] { 1 }) }
            );
        }


        [Fact]
        public async void AddBlockCommitsBlockWhenIsFirstBlock()
        {
            // build a ChainService mock here so that we can mock calls to other functions in the same object
            var chainServiceMock = new Mock<ChainService>(fixture.Context);

            // we aren't testing these methods, so mock them
            chainServiceMock.Setup(m => m.GetLastMainChainBlock()).ReturnsAsync((ValcoinBlock?)null);
            chainServiceMock.Setup(m => m.UpdateBalance(It.IsAny<ValcoinBlock>()));
            chainServiceMock.Setup(m => m.CommitBlock(It.IsAny<ValcoinBlock>()));
            chainServiceMock.Setup(m => m.GetMyWallet()).ReturnsAsync(ValidationServiceShared.MakeTestingWallet());

            // when verifying calls, the reference object must be the same
            var block = GetExampleBlock();


            await chainServiceMock.Object.AddBlock(block);

            chainServiceMock.Verify(m => m.GetLastMainChainBlock(), Times.Once);
            chainServiceMock.Verify(m => m.UpdateBalance(block), Times.Once);
            chainServiceMock.Verify(m => m.CommitBlock(block), Times.Once);
            chainServiceMock.Verify(m => m.GetMyWallet(), Times.Once);
            chainServiceMock.VerifyNoOtherCalls();
        }
    }
}
