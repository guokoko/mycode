using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.TestKit.Xunit2;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Moq;
using TestUtility;
using Xunit;

namespace SharedTests
{
    public class PriceServiceTest : TestKit
    {
        [Fact]
        public async Task PriceService_TotalPriceCount_ShouldSameNumberOfPriceItemInitial()
        {
            //Arrange
            var priceItems = 100;
            var priceStorage = new Mock<IPriceStorage>();
            var logService = new Mock<IEventLogService>();
            var systemLogService = new Mock<ISystemLogService>();
            var defaults = MockUtils.MockOption(new PriceDefaults{ VatRate = 7 });
            var publishConfig = MockUtils.MockOption(new PublishConfiguration(){ StoreChannelMap = new Dictionary<string, string>(){}});
            
            priceStorage.Setup(storage => storage.TotalPriceCount()).ReturnsAsync(priceItems);
            var service = new PriceService(logService.Object, defaults.Object, publishConfig.Object, priceStorage.Object, systemLogService.Object);

            //Act
            var result = await service.TotalPriceCount();
            
            //Assert
            result.Should().Be(priceItems);
        }
    }
}