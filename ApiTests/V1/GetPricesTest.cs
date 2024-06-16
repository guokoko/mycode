using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CTO.Price.Api.Services;
using CTO.Price.Proto.V1;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Moq;
using RZ.Foundation;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ApiTests
{
    public class GetPricesTest
    {
        readonly TestBed<PriceApiServiceV1> testBed;
        public GetPricesTest(ITestOutputHelper output)
        {
            testBed = new TestBed<PriceApiServiceV1>(output);
            testBed.RegisterSingleton<IPerformanceCounter>(new PerformanceCounter());
        }

        [Fact]
        public async Task RequestChannelPrices_BaseAndChannelPriceExists_ReturnSpecialPriceFromChannelPriceAndNormalPriceFromBasePrice()
        {
            //Arrange
            const string bu = "CDS";
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
        
            const decimal baseOriginalPriceVat = 214;
            const decimal baseOriginalPriceNonVat = 200;
            
            const decimal channelOriginalPriceVat = 107;
            const decimal channelOriginalPriceNonVat = 100;
            
            
            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new CTO.Price.Shared.Domain.PriceDescription()
                {
                    Vat = baseOriginalPriceVat,
                    NonVat = baseOriginalPriceNonVat
                }
            }.ToOption();
            
            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
                OriginalPrice = new CTO.Price.Shared.Domain.PriceDescription()
                {
                    Vat = channelOriginalPriceVat,
                    NonVat = channelOriginalPriceNonVat
                }
            }.ToOption();
            
            var getPriceParam = new GetPricesParam()
            {
                Bu = bu,
                Channel = channel,
                Store = store,
                Skus = {sku}
            };
            
            testBed.Fake<IPriceService>()
                .Setup(s => s.GetBaseAndChannelPrices(It.IsAny<IEnumerable<PriceModelKey>>()))
                .ReturnsAsync((new []{new BaseAndChannelPrice(basePrice, channelPrice)}, new string[0]));
        
            var priceApiServiceV1 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV1.GetPrices(getPriceParam, It.IsAny<ServerCallContext>());
            
            //Assert
            result.UnknownSkus.Should().BeEmpty();
            result.Details.Should().HaveCount(1);
            
            var detail = result.Details[0];
            detail.Bu.Should().Be(bu);
            detail.Channel.Should().Be(channel);
            detail.Store.Should().Be(store);
            detail.Sku.Should().Be(sku);
            detail.Details.SpecialPrice.Vat.Should().Be(channelOriginalPriceVat.ToString(CultureInfo.InvariantCulture));
            detail.Details.Price.Vat.Should().Be(baseOriginalPriceVat.ToString(CultureInfo.InvariantCulture));
        }
        
        [Fact]
        public async Task RequestBasePrices_BaseAndChannelPriceExists_ReturnSpecialPriceFromBasePriceAndNormalPriceFromBasePrice()
        {
            //Arrange            
            const string bu = "CDS";
            const string store = "10138";
            const string sku = "CDS-0001";
        
            const decimal baseOriginalPriceVat = 214;
            const decimal baseOriginalPriceNonVat = 200;
            
            const decimal baseSalePriceVat = 107;
            const decimal baseSalePriceNonVat = 100;
            
            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new CTO.Price.Shared.Domain.PriceDescription()
                {
                    Vat = baseOriginalPriceVat,
                    NonVat = baseOriginalPriceNonVat
                },
                SalePrice = new CTO.Price.Shared.Domain.PriceDescription()
                {
                    Vat = baseSalePriceVat,
                    NonVat = baseSalePriceNonVat
                }
            }.ToOption();

            var getPriceParam = new GetPricesParam()
            {
                Bu = bu,
                Store = store,
                Skus = {sku}
            };
            
            var keys = new[] {new PriceModelKey(null, store, sku)};
            
            testBed.Fake<IPriceService>()
                .Setup(s => s.GetBaseAndChannelPrices(keys))
                .ReturnsAsync((new []{new BaseAndChannelPrice(basePrice, Option<PriceModel>.None())}, new string[0]));
        
            var priceApiServiceV1 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV1.GetPrices(getPriceParam, It.IsAny<ServerCallContext>());
            
            //Assert
            result.UnknownSkus.Should().BeEmpty();
            result.Details.Should().HaveCount(1);
            
            var detail = result.Details[0];
            detail.Bu.Should().Be(bu);
            detail.Channel.Should().Be(string.Empty);
            detail.Store.Should().Be(store);
            detail.Sku.Should().Be(sku);
            detail.Details.SpecialPrice.Vat.Should().Be(baseSalePriceVat.ToString(CultureInfo.InvariantCulture));
            detail.Details.Price.Vat.Should().Be(baseOriginalPriceVat.ToString(CultureInfo.InvariantCulture));
        }
        
        [Fact]
        public async Task RequestPrices_NoPrices_ReturnPriceNotFound()
        {
            //Arrange
            const string bu = "CDS";
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";

            var getPriceParam = new GetPricesParam()
            {
                Bu = bu,
                Channel = channel,
                Store = store,
                Skus = {sku}
            };
            
            testBed.Fake<IPriceService>()
                .Setup(s => s.GetBaseAndChannelPrices(It.IsAny<IEnumerable<PriceModelKey>>()))
                .ReturnsAsync((new BaseAndChannelPrice[0], new []{sku}));
        
            var priceApiServiceV1 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV1.GetPrices(getPriceParam, It.IsAny<ServerCallContext>());
            
            //Assert
            result.UnknownSkus.Should().HaveCount(1);
            result.Details.Should().BeEmpty();

            var unknownSku = result.UnknownSkus[0];
            unknownSku.Should().Be(sku);
        }
        
        [Fact]
        public async Task GetPriceMetrics_SendEmptyParam_ReturnPriceDataDetail()
        {
            //Arrange
            const long totalPrices = 1000000;
            const long totalSchedules = 500000;
            const long totalPendingStartSchedules = 1000;
            const long totalPendingEndSchedules = 70000;

            testBed.Fake<IPriceService>().Setup(s => s.TotalPriceCount()).ReturnsAsync(totalPrices);
            testBed.Fake<IScheduleService>().Setup(s => s.TotalScheduleCount()).ReturnsAsync(totalSchedules);
            testBed.Fake<IScheduleService>().Setup(s => s.TotalPendingStartSchedulesCount()).ReturnsAsync(totalPendingStartSchedules);
            testBed.Fake<IScheduleService>().Setup(s => s.TotalPendingEndSchedulesCount()).ReturnsAsync(totalPendingEndSchedules);
            var priceApiServiceV1 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV1.GetPriceMetrics(It.IsAny<Empty>(), It.IsAny<ServerCallContext>());
            
            //Assert
            result.TotalPrices.Should().Be(totalPrices);
            result.TotalSchedules.Should().Be(totalSchedules);
            result.TotalPendingStartSchedules.Should().Be(totalPendingStartSchedules);
            result.TotalPendingEndSchedules.Should().Be(totalPendingEndSchedules);
        }
    }
}