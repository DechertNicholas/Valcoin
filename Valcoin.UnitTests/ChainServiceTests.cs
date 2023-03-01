using Moq;
using Valcoin.Models;
using Valcoin.Services;

namespace Valcoin.UnitTests
{
    public class ChainServiceTests
    {
        private readonly Mock<ValcoinContext> contextMock;
        private readonly Mock<IMiningService> miningMock;
        //private readonly Mock<IChainService> chainServiceMock; // for mocking other calls, keep to one function in unit tests

        public ChainServiceTests()
        {
            contextMock = new Mock<ValcoinContext>();
            miningMock = new Mock<IMiningService>();
            //chainServiceMock = new Mock<IChainService>();
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
                new() { new TxInput("0000", 0, new byte[] { 1 }, new byte[] { 1 }) },
                new() { new TxOutput(50, new byte[] { 1 }) }
            );
        }


        [Fact]
        public async void AddBlockCommitsBlockWhenIsFirstBlock()
        {
            // build a ChainService mock here so that we can mock calls to other functions in the same object
            var chainServiceMock = new Mock<ChainService>(contextMock.Object);

            // we aren't testing these methods, so mock them
            chainServiceMock.Setup(m => m.GetLastMainChainBlock()).ReturnsAsync((ValcoinBlock?)null);
            chainServiceMock.Setup(m => m.UpdateBalance(It.IsAny<ValcoinBlock>()));
            chainServiceMock.Setup(m => m.CommitBlock(It.IsAny<ValcoinBlock>()));

            // when verifying calls, the reference object must be the same
            var block = GetExampleBlock();

            await chainServiceMock.Object.AddBlock(block);

            chainServiceMock.Verify(m => m.GetLastMainChainBlock(), Times.Once);
            chainServiceMock.Verify(m => m.UpdateBalance(block), Times.Once);
            chainServiceMock.Verify(m => m.CommitBlock(block), Times.Once);
            chainServiceMock.VerifyNoOtherCalls();
        }
    }
}
