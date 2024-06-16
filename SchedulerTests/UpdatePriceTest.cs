using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Akka.TestKit.Xunit2;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using RZ.Foundation;
using TestUtility;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;
using UpdateResult = CTO.Price.Shared.UpdateResult;

#pragma warning disable 1998

namespace SchedulerTests
{
    public class UpdatePriceTest : TestKit
    {
        readonly TestBed<PriceService> testBed;

        public UpdatePriceTest(ITestOutputHelper testOutputHelper)
        {
            testBed = new TestBed<PriceService>(testOutputHelper);
        }
        
        [Fact]
        public async Task UpdatePrice_Ignore()
        {
            //Arrange
            var processTime = new DateTime(2020, 08, 05).ToUniversalTime();
            var priceKey = new PriceModelKey("CDS-Website", "10138", "SKU101");

            var storagePrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = (decimal) 150.0,
                    NonVat = (decimal) 150,
                    Start = new DateTime(2020, 07, 27).ToUniversalTime(),
                    End = DateTime.Now.ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 07, 30).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 07, 30).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 08, 06).ToUniversalTime(),
            };

            var incomingPrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = 7,
                LastUpdate = new DateTime(2020, 08, 06).ToUniversalTime(),
            };

            var cachePrice = incomingPrice;
            cachePrice.VatRate = 6;
            
            var logger = Mock.Of<IEventLogService>();
            var defaults = MockUtils.MockOption(new PriceDefaults {VatRate = 7});
            var publishConfig = MockUtils.MockOption(new PublishConfiguration(){ StoreChannelMap = new Dictionary<string, string>(){}});

            var priceStorage = new Mock<IPriceStorage>();
            priceStorage.Setup(p => p.GetPrice(storagePrice.Key)).ReturnsAsync(storagePrice);
            priceStorage
                .Setup(p => p.UpdateDocument(It.IsAny<PriceModel>(), It.IsAny<Expression<Func<PriceModel, bool>>>()))
                .ReturnsAsync(new PriceModel());
            
            var systemLogger = Mock.Of<ISystemLogService>();
            
            var service = new PriceService(logger, defaults.Object, publishConfig.Object, priceStorage.Object, systemLogger);

            //Act
            var (updateResult, updatedPrice) = await service.UpdatePrice(incomingPrice, processTime);

            //Assert
            updateResult.Should().Be(UpdateResult.Ignored);
        }

        [Fact]
        public async Task UpdatePrice_Updated()
        {
            //Arrange
            var processTime = new DateTime(2020, 08, 05).ToUniversalTime();
            var priceKey = new PriceModelKey("CDS-Website", "10138", "SKU101");

            var incomingPrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = (decimal) 800.0,
                    NonVat = (decimal) 700,
                    Start = new DateTime(2020, 07, 27).ToUniversalTime(),
                    End = DateTime.Now.ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 07, 30).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 07, 30).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 07, 24).ToUniversalTime()
            };

            var cachePrice = incomingPrice;
            cachePrice.OriginalPrice.Vat = 450;

            var logger = Mock.Of<IEventLogService>();
            var defaults = MockUtils.MockOption(new PriceDefaults{ VatRate = 7 });
            var publishConfig = MockUtils.MockOption(new PublishConfiguration(){ StoreChannelMap = new Dictionary<string, string>(){}});

            var priceStorage = new Mock<IPriceStorage>();
            priceStorage.Setup(p => p.GetPrice(incomingPrice.Key)).ReturnsAsync(incomingPrice);
            priceStorage
                .Setup(p => p.UpdateDocument(It.IsAny<PriceModel>(), It.IsAny<Expression<Func<PriceModel, bool>>>()))
                .ReturnsAsync(new PriceModel());

            var systemLogger = Mock.Of<ISystemLogService>();
            
            var service = new PriceService(logger, defaults.Object, publishConfig.Object, priceStorage.Object,  systemLogger);

            //Act
            var (updateResult, updatedPrice) = await service.UpdatePrice(incomingPrice, processTime);

            //Assert
            updateResult.Should().Be(UpdateResult.Updated);
        }

        [Fact]
        public async Task UpdatePrice_Deleted()
        {
            //Arrange
            var processTime = new DateTime(2020, 08, 11).ToUniversalTime();
            var priceKey = new PriceModelKey("CDS-Website", "10138", "SKU101");

            var storagePrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = (decimal) 6.5,
                OriginalPrice = new PriceDescription
                {
                    UpdateTime = new DateTime(2020, 07, 30).ToUniversalTime()
                },
                SalePrice = new PriceDescription
                {
                    UpdateTime = new DateTime(2020, 07, 30).ToUniversalTime()
                },
                PromotionPrice = new PriceDescription
                {
                    UpdateTime = new DateTime(2020, 07, 30).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 07, 30).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 07, 31).ToUniversalTime(),
            };

            var incomingPrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = (decimal) 7.5,
                OriginalPrice = new PriceDescription
                {
                    Start = new DateTime(2020, 08, 12).ToUniversalTime(), // start date later than processTime to mark for deletion
                    UpdateTime = new DateTime(2020, 07, 31).ToUniversalTime()
                },
                SalePrice = new PriceDescription
                {
                    Start = new DateTime(2020, 08, 12).ToUniversalTime(), // start date later than processTime to mark for deletion
                    UpdateTime = new DateTime(2020, 07, 31).ToUniversalTime()
                },
                PromotionPrice = new PriceDescription
                {
                    Start = new DateTime(2020, 08, 12).ToUniversalTime(), // start date later than processTime to mark for deletion
                    UpdateTime = new DateTime(2020, 07, 31).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 07, 31).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 08, 07).ToUniversalTime(),
            };

            var cachePrice = incomingPrice;

            var logger = Mock.Of<IEventLogService>();
            var defaults = MockUtils.MockOption(new PriceDefaults{ VatRate = 7 });
            var publishConfig = MockUtils.MockOption(new PublishConfiguration(){ StoreChannelMap = new Dictionary<string, string>(){}});

            var priceStorage = new Mock<IPriceStorage>();
            priceStorage.Setup(p => p.GetPrice(incomingPrice.Key)).ReturnsAsync(storagePrice);
            priceStorage
                .Setup(p => p.DeleteDocument(incomingPrice.Key.ToString(),
                    It.IsAny<Expression<Func<PriceModel, bool>>>())).ReturnsAsync(storagePrice);
            
            var systemLogger = Mock.Of<ISystemLogService>();
            
            var service = new PriceService(logger, defaults.Object, publishConfig.Object, priceStorage.Object, systemLogger);

            //Act
            var (updateResult, updatedPrice) = await service.UpdatePrice(incomingPrice, processTime);

            //Assert
            updateResult.Should().Be(UpdateResult.Deleted);
        }
        
        [Fact]
        public async Task UpdatePrice_Created()
        {
            //Arrange
            var processTime = new DateTime(2020, 08, 05).ToUniversalTime();
            var priceKey = new PriceModelKey("CDS-Website", "10138", "SKU101");

            var incomingPrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = 800,
                    NonVat = 700,
                    Start = new DateTime(2020, 07, 27).ToUniversalTime(),
                    End = DateTime.Now.ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 07, 30).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 07, 30).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 07, 24).ToUniversalTime()
            };

            var logger = Mock.Of<IEventLogService>();
            var defaults = MockUtils.MockOption(new PriceDefaults{ VatRate = 7 });
            var publishConfig = MockUtils.MockOption(new PublishConfiguration(){ StoreChannelMap = new Dictionary<string, string>(){}});

            var priceStorage = new Mock<IPriceStorage>();
            priceStorage.Setup(p => p.GetPrice(incomingPrice.Key)).ReturnsAsync(Option<PriceModel>.None);
            priceStorage
                .Setup(p => p.NewDocument(It.IsAny<PriceModel>()))
                .ReturnsAsync(incomingPrice);

            var systemLogger = Mock.Of<ISystemLogService>();
            
            var service = new PriceService(logger, defaults.Object, publishConfig.Object, priceStorage.Object, systemLogger);

            //Act
            var (updateResult, updatedPrice) = await service.UpdatePrice(incomingPrice, processTime);

            //Assert
            updateResult.Should().Be(UpdateResult.Created);
        }

        [Fact]
        public async Task UpdatePrice_MongoFailsTwice_LogsErrorTwiceAndRetriesUntilSuccessful()
        {
            //Arrange
            var processTime = new DateTime(2020, 08, 05).ToUniversalTime();
            var priceKey = new PriceModelKey( "CDS-Website", "10138", "SKU101");

            var incomingPrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = 800,
                    NonVat = 700,
                    Start = new DateTime(2020, 07, 27).ToUniversalTime(),
                    End = DateTime.Now.ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 07, 30).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 07, 30).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 07, 24).ToUniversalTime()
            };

            testBed.Fake<IOptionsMonitor<PriceDefaults>>().Setup(s => s.CurrentValue).Returns(new PriceDefaults()
            {
                VatRate = 7
            });

            testBed.Fake<IPriceStorage>()
                .SetupSequence(s => s.NewDocument(It.IsAny<PriceModel>()))
                .ThrowsAsync(new PriceServiceException(PriceErrorCategory.DuplicatedCode, "Redis Price creation failed."))
                .ThrowsAsync(new Exception("Random Exception."))
                .ReturnsAsync(incomingPrice);
                

            var systemLogService = new Mock<ISystemLogService>();
            testBed.RegisterSingleton(systemLogService.Object);

            await testBed.CreateSubject().UpdatePrice(incomingPrice, processTime);

            systemLogService.Verify(s => s.Warning(It.IsAny<string>()));
            systemLogService.Verify(s => s.Error(It.IsAny<Exception>(), It.IsAny<string>()));
        }
        
        [Fact]
        public async Task CreatePrice_MongoAndRedisTakeTurnsFailing_RetriesUntilSuccessful()
        {
            //Arrange
            var processTime = new DateTime(2020, 08, 05).ToUniversalTime();
            var priceKey = new PriceModelKey( "CDS-Website", "10138", "SKU101");

            var incomingPrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = 800,
                    NonVat = 700,
                    Start = new DateTime(2020, 07, 27).ToUniversalTime(),
                    End = DateTime.Now.ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 07, 30).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 07, 30).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 07, 24).ToUniversalTime()
            };

            testBed.Fake<IOptionsMonitor<PriceDefaults>>().Setup(s => s.CurrentValue).Returns(new PriceDefaults()
            {
                VatRate = 7
            });

            testBed.Fake<IPriceStorage>()
                .SetupSequence(s => s.NewDocument(It.IsAny<PriceModel>()))
                .ThrowsAsync(new PriceServiceException(PriceErrorCategory.DuplicatedCode, "Redis Price creation failed."))
                .ReturnsAsync(incomingPrice)
                .ThrowsAsync(new Exception("Random Exception."))
                .ReturnsAsync(incomingPrice)
                .ReturnsAsync(incomingPrice);

            var systemLogService = new Mock<ISystemLogService>();
            testBed.RegisterSingleton(systemLogService.Object);

            var result = await testBed.CreateSubject().UpdatePrice(incomingPrice, processTime);

            result.Item1.Should().Be(UpdateResult.Created);
        }
        
        [Fact]
        public async Task UpdatePrice_MongoAndRedisTakeTurnsFailing_RetriesUntilSuccessful()
        {
            //Arrange
            var processTime = new DateTime(2020, 08, 05).ToUniversalTime();
            var priceKey = new PriceModelKey( "CDS-Website", "10138", "SKU101");
            
            var existingPrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = 800,
                    NonVat = 700,
                    Start = new DateTime(2020, 07, 27).ToUniversalTime(),
                    End = new DateTime(2020, 09, 27).ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 07, 30).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 07, 30).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 07, 24).ToUniversalTime()
            };

            var incomingPrice = new PriceModel()
            {
                Key = priceKey,
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = 800,
                    NonVat = 700,
                    Start = new DateTime(2020, 07, 27).ToUniversalTime(),
                    End = new DateTime(2020, 09, 27).ToUniversalTime(),
                    UpdateTime = new DateTime(2020, 07, 30).ToUniversalTime()
                },
                PriceTime = new DateTime(2020, 08, 1).ToUniversalTime(),
                LastUpdate = new DateTime(2020, 07, 24).ToUniversalTime()
            };

            testBed.Fake<IOptionsMonitor<PriceDefaults>>().Setup(s => s.CurrentValue).Returns(new PriceDefaults()
            {
                VatRate = 7
            });

            testBed.Fake<IPriceStorage>()
                .Setup(s => s.GetPrice(It.IsAny<PriceModelKey>()))
                .ReturnsAsync(Option<PriceModel>.From(existingPrice));

            testBed.Fake<IPriceStorage>()
                .SetupSequence(s => s.UpdateDocument(It.IsAny<PriceModel>(), It.IsAny<Expression<Func<PriceModel, bool>>>()))
                .ThrowsAsync(new PriceServiceException(PriceErrorCategory.UpdateFailed, "Mongo update failed."))
                .ReturnsAsync(incomingPrice)
                .ThrowsAsync(new Exception("Random Exception."))
                .ReturnsAsync(incomingPrice)
                .ReturnsAsync(incomingPrice);

            var systemLogService = new Mock<ISystemLogService>();
            testBed.RegisterSingleton(systemLogService.Object);

            var result = await testBed.CreateSubject().UpdatePrice(incomingPrice, processTime);

            result.Item1.Should().Be(UpdateResult.Updated);
        }
    }
}