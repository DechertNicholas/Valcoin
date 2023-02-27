using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
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
        public async void AddBlockAddsBlockWhenIsFirstBlock()
        {
            // build a ChainService mock here so that we can mock calls to other functions in the same object
            //var chainServiceMock = new Mock<ChainService>(miningMock.Object, contextMock.Object);

            // we aren't testing these methods, so mock them
            //chainServiceMock.Setup(m => m.GetLastMainChainBlock()).ReturnsAsync((ValcoinBlock?)null);
            //chainServiceMock.Setup(m => m.CommitBlock(It.IsAny<ValcoinBlock>()));
            //contextMock.Setup(c => c.ValcoinBlocks.MaxAsync(It.IsAny<Expression<Func<ValcoinBlock?, ValcoinBlock?>>>(), default)).ReturnsAsync((ValcoinBlock?)null);
            //contextMock.Setup(c => c.ValcoinBlocks.Where(It.IsAny<Expression<Func<ValcoinBlock?, bool>>>())).Returns(IQueryable<ValcoinBlock>(null));

            // setup LINQ mocks
            var data = new List<ValcoinBlock>().AsQueryable();
            var mockSet = new Mock<DbSet<ValcoinBlock>>();
            mockSet.As<IQueryable<ValcoinBlock>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<ValcoinBlock>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<ValcoinBlock>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<ValcoinBlock>>().Setup(m => m.GetEnumerator()).Returns(() => data.GetEnumerator());

            // the context will return an empty list
            // TODO: This is broken
            contextMock.Setup(c => c.ValcoinBlocks).Returns(mockSet.Object);

            // when verifying calls, the reference object must be the same
            var block = GetExampleBlock();

            await new ChainService(miningMock.Object, contextMock.Object).AddBlock(block);

            //await new ChainService(miningMock.Object, contextMock.Object).AddBlock(block);

            //storageMock.Verify(m => m.GetLastMainChainBlock(), Times.Once);
            //storageMock.Verify(m => m.AddBlock(block), Times.Once);
            //storageMock.VerifyNoOtherCalls();
            //miningMock.VerifyNoOtherCalls();
        }
    }
}
