using System;
using System.Threading.Tasks;
using CTO.Price.Api.Services;
using CTO.Price.Proto.V1;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Moq;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ApiTests
{
    public class UpdatePriceTest
    {
        readonly TestBed<PriceApiServiceV1> testBed;

        public UpdatePriceTest(ITestOutputHelper output)
        {
            testBed = new TestBed<PriceApiServiceV1>(output);
            testBed.RegisterSingleton<IPerformanceCounter>(new PerformanceCounter());
        }
        
        [Fact]
        public async Task UpdatePrice_BaseAndChannelPriceExists_PriceBeUpdated()
        {
            //Arrange
            const string bu = "CDS";
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
            const string vatRate = "7";
            var start = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2020, 10, 31, 0, 0, 0, DateTimeKind.Utc);
        
            const string baseOriginalPriceVat = "214";
            const string baseOriginalPriceNonVat = "200";

            const string salePriceVat = "107";
            const string salePriceNonVat = "100";

            const string promotionNonVat = "70";
            
            var priceUpdateParam = new PriceUpdateParam
            {
                Bu = bu,
                Channel = channel,
                Store = store,
                Sku = sku,
                VatRate = vatRate,
                OriginalPrice = new PriceDescription
                {
                    PriceVat = baseOriginalPriceVat,
                    PriceNonVat = baseOriginalPriceNonVat,
                    Start = start.ToTimestamp(),
                    End = end.ToTimestamp()
                },
                SalePrice = new PriceDescription
                {
                    PriceVat = salePriceVat,
                    PriceNonVat = salePriceNonVat
                },
                PromotionPrice = new PriceDescription
                {
                    PriceNonVat = promotionNonVat
                }
            };

            testBed.Fake<ITopicPublisher>()
                .Setup(s => s.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<TimeSpan>()));
            testBed.Fake<IOptions<MessageBusOption>>()
                .Setup(s => s.Value).Returns(new MessageBusOption
                {
                    WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
                });
            
            var priceApiServiceV1 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV1.UpdatePrice(priceUpdateParam, It.IsAny<ServerCallContext>());
            
            //Assert
            result.GetType().Should().Be(typeof(Empty));
        }

        [Fact]
        public async Task UpdatePrice_ParamChannelIsEmpty_ThrowMissingChannel()
        {
            //Arrange
            const string bu = "CDS";
            const string store = "10138";
            const string sku = "CDS-0001";
            const string vatRate = "7";
        
            const string baseOriginalPriceVat = "214";
            const string baseOriginalPriceNonVat = "200";
            
            var priceUpdateParam = new PriceUpdateParam
            {
                Bu = bu,
                Channel = string.Empty,
                Store = store,
                Sku = sku,
                VatRate = vatRate,
                OriginalPrice = new PriceDescription
                {
                    PriceVat = baseOriginalPriceVat,
                    PriceNonVat = baseOriginalPriceNonVat
                }
            };
            var priceApiServiceV1 = testBed.CreateSubject();

            var expectExpDetail = "Invalid payload detected, Missing Channel parameter.";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV1.UpdatePrice(priceUpdateParam, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument
                                                                       && e.Status.Detail == expectExpDetail);
        }

        [Fact]
        public async Task UpdatePrice_ParamStoreIsEmpty_ThrowMissingStore()
        {
            //Arrange
            const string bu = "CDS";
            const string channel = "CDS-Website";
            const string sku = "CDS-0001";
            const string vatRate = "7";
        
            const string baseOriginalPriceVat = "214";
            const string baseOriginalPriceNonVat = "200";
            
            var priceUpdateParam = new PriceUpdateParam
            {
                Bu = bu,
                Channel = channel,
                Store = string.Empty,
                Sku = sku,
                VatRate = vatRate,
                OriginalPrice = new PriceDescription
                {
                    PriceVat = baseOriginalPriceVat,
                    PriceNonVat = baseOriginalPriceNonVat
                }
            };
            var priceApiServiceV1 = testBed.CreateSubject();

            var expectExpDetail = "Invalid payload detected, Missing Store parameter.";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV1.UpdatePrice(priceUpdateParam, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument
                                                                       && e.Status.Detail == expectExpDetail);
        }

        [Fact]
        public async Task UpdatePrice_ParamSkuIsEmpty_ThrowMissingSku()
        {
            //Arrange
            const string bu = "CDS";
            const string channel = "CDS-Website";
            const string store = "10138";
            const string vatRate = "7";
        
            const string baseOriginalPriceVat = "214";
            const string baseOriginalPriceNonVat = "200";
            
            var priceUpdateParam = new PriceUpdateParam
            {
                Bu = bu,
                Channel = channel,
                Store = store,
                Sku = string.Empty,
                VatRate = vatRate,
                OriginalPrice = new PriceDescription
                {
                    PriceVat = baseOriginalPriceVat,
                    PriceNonVat = baseOriginalPriceNonVat
                }
            };
            var priceApiServiceV1 = testBed.CreateSubject();

            var expectExpDetail = "Invalid payload detected, Missing SKU parameter.";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV1.UpdatePrice(priceUpdateParam, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument
                                                                       && e.Status.Detail == expectExpDetail);
        }
        
        [Fact]
        public async Task UpdatePrice_ParamMissingEveryPrice_ThrowKeyNoPrice()
        {
            //Arrange
            const string bu = "CDS";
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
            const string vatRate = "7";
        
            var priceUpdateParam = new PriceUpdateParam
            {
                Bu = bu,
                Channel = channel,
                Store = store,
                Sku = sku,
                VatRate = vatRate,
            };
            var priceApiServiceV1 = testBed.CreateSubject();

            var expectExpDetail = "Invalid payload detected, CDS-Website.10138:CDS-0001 has no prices.";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV1.UpdatePrice(priceUpdateParam, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument
                                                                       && e.Status.Detail == expectExpDetail);
        }
        
        [Fact]
        public async Task UpdatePrice_ParamMissingEveryPrice_ThrowOriginalPriceNoPrice()
        {
            //Arrange
            const string bu = "CDS";
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
            const string vatRate = "7";
            
            var priceUpdateParam = new PriceUpdateParam
            {
                Bu = bu,
                Channel = channel,
                Store = store,
                Sku = sku,
                VatRate = vatRate,
                OriginalPrice = new PriceDescription { }
            };
            var priceApiServiceV1 = testBed.CreateSubject();

            var expectExpDetail = "Invalid payload detected, OriginalPrice field contains no prices.";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV1.UpdatePrice(priceUpdateParam, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument
                                                                       && e.Status.Detail == expectExpDetail);
        }
    }
}