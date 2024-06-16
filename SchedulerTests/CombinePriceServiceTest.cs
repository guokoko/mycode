using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CTO.Price.Shared;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Moq;
using RZ.Foundation;
using TestUtility;
using Xunit;
using DateTime = System.DateTime;

namespace SchedulerTests
{
    public class CombinePriceServiceTest
    {
        [Fact]
        public async Task CombinePrice_OnePriceKeepingOnePriceIncoming_ShouldCombineTwoPrice() {
            //mock Date
            var processTime = new DateTime(2020, 05, 15).ToUniversalTime();
            var priceKey = new PriceModelKey("CDS-Website", "10138", "123456");
            var keepPrice = new PriceModel
            {
                Key = priceKey,
                VatRate = 7,
                SalePrice = new PriceDescription
                {
                    Vat = 214,
                    NonVat = 200,
                    Start = new DateTime(2020, 05, 01).ToUniversalTime(),
                    End = new DateTime(2020, 12, 31).ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 05, 01).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 05, 20).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 05, 20).ToUniversalTime()
            };
            var incomingPrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    NonVat = 100,
                    Start = new DateTime(2020, 05, 01).ToUniversalTime(),
                    End = new DateTime(2020, 12, 31).ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 05, 22).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 05, 22).ToUniversalTime()
            };
            
            // mock service
            var logger = Mock.Of<IEventLogService>();
            var systemlogger = Mock.Of<ISystemLogService>();
            var defaults = MockUtils.MockOption(new PriceDefaults{ VatRate = 7 });
            var publishConfig = MockUtils.MockOption(new PublishConfiguration(){ StoreChannelMap = new Dictionary<string, string>(){}});
            
            var priceStorage = new Mock<IPriceStorage>();
            priceStorage.Setup(p => p.GetPrice(priceKey)).ReturnsAsync(keepPrice);
            priceStorage.Setup(p => p.UpdateDocument(It.IsAny<PriceModel>(), It.IsAny<Expression<Func<PriceModel, bool>>>())).ReturnsAsync(new PriceModel());

            var systemLogger = Mock.Of<ISystemLogService>();
            
            var service = new PriceService(logger, defaults.Object, publishConfig.Object, priceStorage.Object, systemLogger);

            //Assert
            var (updateResult, updatedPrice) = await service.UpdatePrice(incomingPrice, processTime);
            updateResult.Should().Be(UpdateResult.Updated);
            var price = updatedPrice.Get();
            price.OriginalPrice?.Vat.Should().Be(107);
            price.OriginalPrice?.UpdateTime.Should().Be(new DateTime(2020, 05, 22).ToUniversalTime());
            price.SalePrice?.Vat.Should().Be(214);
            price.SalePrice?.UpdateTime.Should().Be(new DateTime(2020, 05, 01).ToUniversalTime());
            price.PromotionPrice.Should().BeNull();
        }

        [Fact]
        public async Task CombinePrice_TwoPriceKeepingOneSamePriceIncoming_ShouldCombineTwoPrice() {
            //mock Date
            var processTime = new DateTime(2020, 05, 15).ToUniversalTime();
            var priceKey = new PriceModelKey("CDS-Website", "10138", "123456");
            var keepPrice = new PriceModel
            {
                Key = priceKey,
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = 50,
                    Start = new DateTime(2020, 05, 01).ToUniversalTime(),
                    End = new DateTime(2020, 12, 31).ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 05, 01).ToUniversalTime()
                },
                SalePrice = new PriceDescription
                {
                    Vat = 107,
                    NonVat = 100,
                    Start = new DateTime(2020, 05, 01).ToUniversalTime(),
                    End = new DateTime(2020, 12, 31).ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 05, 01).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 05, 20).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 05, 20).ToUniversalTime()
            };
            var incomingPrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = 7,
                OriginalPrice = new PriceDescription 
                {
                    Vat = 53.5m,
                    NonVat = 50,
                    Start = new DateTime(2020, 05, 01).ToUniversalTime(),
                    End = new DateTime(2020, 12, 31).ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 05, 25).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 05, 21).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 05, 25).ToUniversalTime()
            };
            
            // mock service
            var logger = Mock.Of<IEventLogService>();
            var systemlogger = Mock.Of<ISystemLogService>();
            var defaults = MockUtils.MockOption(new PriceDefaults{ VatRate = 7 });
            var publishConfig = MockUtils.MockOption(new PublishConfiguration(){ StoreChannelMap = new Dictionary<string, string>(){}});
            
            var priceStorage = new Mock<IPriceStorage>();
            priceStorage.Setup(p => p.GetPrice(priceKey)).ReturnsAsync(keepPrice);
            priceStorage.Setup(p => p.UpdateDocument(It.IsAny<PriceModel>(), It.IsAny<Expression<Func<PriceModel, bool>>>())).ReturnsAsync(new PriceModel());
            var systemLogger = Mock.Of<ISystemLogService>();
            
            var service = new PriceService(logger, defaults.Object, publishConfig.Object, priceStorage.Object, systemlogger);
            
            //Act
            var (updateResult, updatedPrice) = await service.UpdatePrice(incomingPrice, processTime);

            //Assert
            updateResult.Should().Be(UpdateResult.Updated);
            var price = updatedPrice.Get();
            price.OriginalPrice?.Vat.Should().Be(53.5m);
            price.OriginalPrice?.UpdateTime.Should().Be(new DateTime(2020, 05, 25).ToUniversalTime());
            price.SalePrice?.Vat.Should().Be(107);
            price.SalePrice?.UpdateTime.Should().Be(new DateTime(2020, 05, 01).ToUniversalTime());
            price.PromotionPrice.Should().BeNull();
        }

        [Fact]
        public async Task CombinePrice_OnePriceKeepingOneOutdatedPriceIncoming_ShouldNotUpdatePrice() {
            //mock Date
            var processTime = new DateTime(2020, 05, 15).ToUniversalTime();
            var priceKey = new PriceModelKey("CDS-Website", "10138", "123456");
            var keepPrice = new PriceModel
            {
                Key = priceKey,
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    NonVat = 100,
                    Start = new DateTime(2020, 05, 01).ToUniversalTime(),
                    End = new DateTime(2020, 12, 31).ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 05, 02).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 05, 01).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 05, 01).ToUniversalTime()
            };
            var incomingPrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = 214,
                    NonVat = 200,
                    Start = new DateTime(2020, 05, 01).ToUniversalTime(),
                    End = new DateTime(2020, 12, 31).ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 05, 01).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 05, 01).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 05, 01).ToUniversalTime()
            };
            
            // mock service
            var logger = Mock.Of<IEventLogService>();
            var systemlogger = Mock.Of<ISystemLogService>();
            var defaults = MockUtils.MockOption(new PriceDefaults{ VatRate = 7 });
            var publishConfig = MockUtils.MockOption(new PublishConfiguration(){ StoreChannelMap = new Dictionary<string, string>(){}});
            
            var priceStorage = new Mock<IPriceStorage>();
            priceStorage.Setup(p => p.GetPrice(priceKey)).ReturnsAsync(keepPrice);
            priceStorage.Setup(p => p.UpdateDocument(It.IsAny<PriceModel>(), It.IsAny<Expression<Func<PriceModel, bool>>>())).ReturnsAsync(new PriceModel());

            var service = new PriceService(logger, defaults.Object, publishConfig.Object, priceStorage.Object, systemlogger);
            
            // Act
            var (updateResult, updatedPrice) = await service.UpdatePrice(incomingPrice, processTime);
            
            //Assert
            updatedPrice.IsNone.Should().BeTrue();
            updateResult.Should().Be(UpdateResult.Ignored);
        }
    }
}