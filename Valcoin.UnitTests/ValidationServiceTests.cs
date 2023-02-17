using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Services;
using Valcoin.UnitTests.SharedData;

namespace Valcoin.UnitTests
{
    public class ValidationServiceTests
    {
        [Fact]
        public async void ValidateTx()
        {
            var serviceMock = new Mock<IStorageService>();
            serviceMock.Setup(s => s.GetTx(It.IsAny<string>())).ReturnsAsync(ValidationServiceShared.ValidCoinbaseOnlyBlock.Transactions[0]);

            var result = await ValidationService.ValidateTx(ValidationServiceShared.ValidSpendBlock.Transactions[1], serviceMock.Object);

            Assert.Equal(ValidationService.ValidationCode.Valid, result);
        }
    }
}
