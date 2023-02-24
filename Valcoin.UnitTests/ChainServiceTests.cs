using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;

namespace Valcoin.UnitTests
{
    public class ChainServiceTests
    {
        private readonly Mock<IStorageService> storageMock;
        private readonly Mock<IMiningService> miningMock;

        public ChainServiceTests()
        {
            storageMock = new Mock<IStorageService>();
            miningMock = new Mock<IMiningService>();
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
        public async void CallsGetLastMainChainBlock()
        {
            storageMock.Setup(s => s.GetLastMainChainBlock()).ReturnsAsync(GetExampleBlock());

            var result = await new ChainService(storageMock.Object, miningMock.Object).GetLastMainChainBlock();

            Assert.Equal(1, storageMock.Invocations.Count);
        }

        [Fact]
        public async void CallsGetBlock()
        {
            storageMock.Setup(s => s.GetBlock(It.IsAny<string>())).ReturnsAsync(GetExampleBlock());

            var result = await new ChainService(storageMock.Object, miningMock.Object).GetBlock("test");

            Assert.Equal(1, storageMock.Invocations.Count);
        }

        [Fact]
        public async void CallsGetTx()
        {
            storageMock.Setup(s => s.GetTx(It.IsAny<string>())).ReturnsAsync(GetExampleTransaction());

            var result = await new ChainService(storageMock.Object, miningMock.Object).GetTx("test");

            Assert.Equal(1, storageMock.Invocations.Count);
        }

        [Fact]
        public async void CallsGetTxByInput()
        {
            storageMock.Setup(s => s.GetTxByInput(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(GetExampleTransaction());

            var result = await new ChainService(storageMock.Object, miningMock.Object).GetTxByInput("test", 0);

            Assert.Equal(1, storageMock.Invocations.Count);
        }

        [Fact]
        public async void AddBlockAddsBlockWhenIsFirstBlock()
        {
            storageMock.Setup(s => s.GetLastMainChainBlock()).ReturnsAsync(default(ValcoinBlock));
            storageMock.Setup(s => s.AddBlock(It.IsAny<ValcoinBlock>()));

            // when verifying calls, the reference object must be the same
            var block = GetExampleBlock();

            await new ChainService(storageMock.Object, miningMock.Object).AddBlock(block);

            storageMock.Verify(m => m.GetLastMainChainBlock(), Times.Once);
            storageMock.Verify(m => m.AddBlock(block), Times.Once);
            storageMock.VerifyNoOtherCalls();
            miningMock.VerifyNoOtherCalls();
        }
    }
}
