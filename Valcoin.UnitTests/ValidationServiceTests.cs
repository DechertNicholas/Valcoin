using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;
using Valcoin.UnitTests.SharedData;

namespace Valcoin.UnitTests
{
    public class ValidationServiceTests
    {
        [Fact]
        public void ValidateTx()
        {
            var serviceMock = new Mock<IChainService>();
            serviceMock
                .Setup(s => s.GetTx(It.IsAny<string>()))
                .ReturnsAsync(ValidationServiceShared.ValidCoinbaseOnlyBlock.Transactions[0]);

            var result = ValidationService.ValidateTx(ValidationServiceShared.ValidSpendBlock.Transactions[1], serviceMock.Object);

            Assert.Equal(ValidationService.ValidationCode.Valid, result);
        }

        [Fact]
        public void InvalidateAlreadySpentTransaction()
        {
            var serviceMock = new Mock<IChainService>();

            serviceMock
                .Setup(s => s.GetTxByInput(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(ValidationServiceShared.ValidCoinbaseOnlyBlock.Transactions[0]);

            var result = ValidationService.ValidateTx(ValidationServiceShared.ValidSpendBlock.Transactions[1], serviceMock.Object);

            Assert.Equal(ValidationService.ValidationCode.Invalid, result);
        }
    }
}
