using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.TestKit.Xunit2;
using CTO.Price.Api.Services;
using CTO.Price.Proto.V2;
using CTO.Price.Shared;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Extensions;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Moq;
using RZ.Foundation;
using TestUtility;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using PriceDescription = CTO.Price.Proto.V2.PriceDescription;

namespace ApiTests
{
    public class PriceApiServiceV2Test : TestKit
    {
        readonly TestBed<PriceApiServiceV2> testBed;
        public PriceApiServiceV2Test(ITestOutputHelper output)
        {
            testBed = new TestBed<PriceApiServiceV2>(output);
            testBed.RegisterSingleton<IPerformanceCounter>(new PerformanceCounter());
        }

        #region GetPricesBySku

        [Fact]
        public async Task RequestBasePrices_BaseAndChannelPriceExists_ReturnPriceBySku()
        {
            //Arrange
            const string channel = "CDS";
            const string store = "10138";
            const string sku = "CDS-0001";
        
            const decimal baseOriginalPriceVat = 214;
            const decimal baseOriginalPriceNonVat = 200;
            
            const decimal baseSalePriceVat = 107;
            const decimal baseSalePriceNonVat = 100;
            
            var price = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
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

            var getPricesBySkuParam = new GetPricesBySkuParam()
            {
                Sku = sku
            };

            var systemKeys = new[] {new PriceModelKey(null, store, sku)};
            
            testBed.Fake<IPriceService>()
                .Setup(s => s.GetPriceModelKeys(It.IsAny<string>()))
                .ReturnsAsync(systemKeys);
            
            testBed.Fake<IPriceService>()
                .Setup(s => s.GetBaseAndChannelPrices(systemKeys))
                .ReturnsAsync((new []{new BaseAndChannelPrice(price, Option<PriceModel>.None())}, new string[0]));
        
            var priceApiServiceV2 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV2.GetPricesBySku(getPricesBySkuParam, It.IsAny<ServerCallContext>());
            
            //Assert
            result.UnknownSkus.Should().BeEmpty();
            result.Details.Should().HaveCount(1);
            
            var detail = result.Details[0];
            detail.Channel.Should().Be(channel);
            detail.Store.Should().Be(store);
            detail.Sku.Should().Be(sku);
            detail.Details.SpecialPrice.Vat.Should().Be(baseSalePriceVat.ToString(CultureInfo.InvariantCulture));
            detail.Details.Price.Vat.Should().Be(baseOriginalPriceVat.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

        #region GetPricesByStore

        [Fact]
        public async Task RequestBasePrices_BaseAndChannelPriceExists_ReturnPriceByStore()
        {
            //Arrange
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

            var getPricesByStoreParam = new GetPricesByStoreParam()
            {
                Store   = store,
                Skus = {sku}
            };
            
            var keys = new[] {new PriceModelKey(null, store, sku)};

            testBed.Fake<IPriceService>()
                .Setup(s => s.GetBaseAndChannelPrices(keys))
                .ReturnsAsync((new []{new BaseAndChannelPrice(basePrice, Option<PriceModel>.None())}, new string[0]));
        
            var priceApiServiceV2 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV2.GetPricesByStore(getPricesByStoreParam, It.IsAny<ServerCallContext>());
            
            //Assert
            result.UnknownSkus.Should().BeEmpty();
            result.Details.Should().HaveCount(1);
            
            var detail = result.Details[0];
            detail.Channel.Should().Be(string.Empty);
            detail.Store.Should().Be(store);
            detail.Sku.Should().Be(sku);
            detail.Details.SpecialPrice.Vat.Should().Be(baseSalePriceVat.ToString(CultureInfo.InvariantCulture));
            detail.Details.Price.Vat.Should().Be(baseOriginalPriceVat.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

        #region GetPricesByChannel

        [Fact]
        public async Task RequestBasePrices_BaseAndChannelPriceExists_ReturnPriceByChannel()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
        
            const decimal baseOriginalPriceVat = 214;
            const decimal baseOriginalPriceNonVat = 200;
            
            const decimal baseSalePriceVat = 107;
            const decimal baseSalePriceNonVat = 100;
            
            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
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

            var getPricesByChannelParam = new GetPricesByChannelParam()
            {
                Channel = channel,
                Store   = store,
                Skus = {sku}
            };
            
            var keys = new[] {new PriceModelKey(channel, store, sku)};

            testBed.Fake<IPriceService>()
                .Setup(s => s.GetBaseAndChannelPrices(keys))
                .ReturnsAsync((new []{new BaseAndChannelPrice(basePrice, Option<PriceModel>.None())}, new string[0]));
        
            var priceApiServiceV2 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV2.GetPricesByChannel(getPricesByChannelParam, It.IsAny<ServerCallContext>());
            
            //Assert
            result.UnknownSkus.Should().BeEmpty();
            result.Details.Should().HaveCount(1);
            
            var detail = result.Details[0];
            detail.Channel.Should().Be(channel);
            detail.Store.Should().Be(store);
            detail.Sku.Should().Be(sku);
            detail.Details.SpecialPrice.Vat.Should().Be(baseSalePriceVat.ToString(CultureInfo.InvariantCulture));
            detail.Details.Price.Vat.Should().Be(baseOriginalPriceVat.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

        #region GetSchedules

        [Fact]
        public async Task RequestScheduler_SchedulerExists_ReturnScheduler()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
            var start = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2020, 10, 31, 0, 0, 0, DateTimeKind.Utc);
            var lastUpdate = new DateTime(2020, 9, 30, 0, 0, 0, DateTimeKind.Utc);

            const decimal baseOriginalPriceVat = 214;
            const decimal baseOriginalPriceNonVat = 200;
            
            var schedulePrice = new SchedulePriceUpdate
            {
                OriginalPrice = new CTO.Price.Shared.Domain.PriceDescription
                {
                    Vat = baseOriginalPriceVat,
                    NonVat = baseOriginalPriceNonVat
                }
            };

            var schedule = new Schedule
            {
                Key = new ScheduleKey(start, end, channel, store, sku),
                PriceUpdate = schedulePrice,
                Status = ScheduleStatus.Completed,
                LastUpdate = lastUpdate
            };
            async IAsyncEnumerable<Schedule> GetMockSchedules()
            {
                yield return schedule;
                await Task.CompletedTask;
            }

            var getSchedulesParam = new GetSchedulesParam()
            {
                Channel = channel,
                Store = store,
                Sku = sku
            };

            testBed.Fake<IScheduleService>()
                .Setup(s => s.GetSchedules(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(GetMockSchedules());
            var priceApiServiceV2 = testBed.CreateSubject();
            
            //Act
            var stream = new ServerStreamMock<GetSchedulesReply>();
            await priceApiServiceV2.GetSchedules(getSchedulesParam, stream, It.IsAny<ServerCallContext>());

            //Assert
            var result = stream.Messages.FirstOrDefault();
            result?.Start.Should().Be(start.ToTimestamp());
            result?.End.Should().Be(end.ToTimestamp());
            result?.OriginalPriceSchedule.Vat.Should()
                .Be(baseOriginalPriceVat.ToString(CultureInfo.InvariantCulture));
            result?.OriginalPriceSchedule.NonVat.Should()
                .Be(baseOriginalPriceNonVat.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

        #region DeleteSchedule
        [Fact]
        public async Task DeleteSchedule_DeleteScheduleSuccess_ShouldReturnEmpty()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
            var start = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2020, 10, 31, 0, 0, 0, DateTimeKind.Utc);
            
            var deleteScheduleParam = new DeleteScheduleParam
            {
                Channel = channel,
                Store = store,
                Sku = sku,
                Start = start.ToTimestamp(),
                End = end.ToTimestamp()
            };

            testBed.Fake<IScheduleService>()
                .Setup(s => s.DeleteSchedule(It.IsAny<ScheduleKey>())).ReturnsAsync(UpdateResult.Deleted);
            
            var priceApiServiceV2 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV2.DeleteSchedule(deleteScheduleParam, It.IsAny<ServerCallContext>());
            
            //Assert
            result.GetType().Should().Be(typeof(Empty));
        }

        [Fact]
        public async Task DeleteSchedule_DeleteScheduleFailure_ShouldThrowRpcException()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
            var start = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2020, 10, 31, 0, 0, 0, DateTimeKind.Utc);
            
            var deleteScheduleParam = new DeleteScheduleParam
            {
                Channel = channel,
                Store = store,
                Sku = sku,
                Start = start.ToTimestamp(),
                End = end.ToTimestamp()
            };
            var key = new ScheduleKey(start, end, channel, store, sku);

            testBed.Fake<IScheduleService>()
                .Setup(s => s.DeleteSchedule(It.IsAny<ScheduleKey>()))
                .Throws(new ConstraintException(
                    $"Schedule {key} can not be deleted. Only inactive schedules can be deleted."));
            
            var priceApiServiceV2 = testBed.CreateSubject();
            
            var expectExpDetail = "Schedule 2020-10-01 00:00:00Z.2020-10-31 00:00:00Z.CDS-Website.10138.CDS-0001 can not be deleted. Only inactive schedules can be deleted.";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV2.DeleteSchedule(deleteScheduleParam, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.FailedPrecondition
                                                                       && e.Status.Detail == expectExpDetail);
        }
        #endregion
        
        #region GetPriceMetrics
        [Fact]
        public async Task PriceApiServiceV2_GetPriceMetrics_ReturnMetricsAsInitial()
        {
            //Arrange
            const long totalPriceCount = 1000;
            const long totalScheduleCount = 1500;
            const long totalPendingStartSchedulesCount = 200;
            const long totalPendingEndSchedulesCount = 300;
            
            testBed.Fake<IPriceService>().Setup(s => s.TotalPriceCount())
                .ReturnsAsync(totalPriceCount);
            
            testBed.Fake<IScheduleService>().Setup(s => s.TotalScheduleCount())
                .ReturnsAsync(totalScheduleCount);
            
            testBed.Fake<IScheduleService>().Setup(s => s.TotalPendingStartSchedulesCount())
                .ReturnsAsync(totalPendingStartSchedulesCount);
            
            testBed.Fake<IScheduleService>().Setup(s => s.TotalPendingEndSchedulesCount())
                .ReturnsAsync(totalPendingEndSchedulesCount);
        
            var priceApiServiceV2 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV2.GetPriceMetrics(It.IsAny<Empty>(), It.IsAny<ServerCallContext>());
            
            //Assert
            result.TotalPrices.Should().Be(totalPriceCount);
            result.TotalSchedules.Should().Be(totalScheduleCount);
            result.TotalPendingStartSchedules.Should().Be(totalPendingStartSchedulesCount);
            result.TotalPendingEndSchedules.Should().Be(totalPendingEndSchedulesCount);
        }
        
        #endregion

        #region UpdatePrice

        [Fact]
        public async Task UpdatePrice_BaseAndChannelPriceExists_PriceBeUpdated()
        {
            //Arrange
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
            
            var priceApiServiceV2 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV2.UpdatePrice(priceUpdateParam, It.IsAny<ServerCallContext>());
            
            //Assert
            result.GetType().Should().Be(typeof(Empty));
        }

        [Fact]
        public async Task UpdatePrice_ParamStoreIsEmpty_ThrowMissingStore()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string sku = "CDS-0001";
            const string vatRate = "7";
        
            const string baseOriginalPriceVat = "214";
            const string baseOriginalPriceNonVat = "200";
            
            var priceUpdateParam = new PriceUpdateParam
            {
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
            var priceApiServiceV2 = testBed.CreateSubject();

            var expectExpDetail = "Invalid payload detected, Missing Store parameter.";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV2.UpdatePrice(priceUpdateParam, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument
                                                                       && e.Status.Detail == expectExpDetail);
        }

        [Fact]
        public async Task UpdatePrice_ParamSkuIsEmpty_ThrowMissingSku()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string vatRate = "7";
        
            const string baseOriginalPriceVat = "214";
            const string baseOriginalPriceNonVat = "200";
            
            var priceUpdateParam = new PriceUpdateParam
            {
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
            var priceApiServiceV2 = testBed.CreateSubject();

            var expectExpDetail = "Invalid payload detected, Missing SKU parameter.";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV2.UpdatePrice(priceUpdateParam, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument
                                                                       && e.Status.Detail == expectExpDetail);
        }
        
        [Fact]
        public async Task UpdatePrice_ParamMissingEveryPrice_ThrowKeyNoPrice()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
            const string vatRate = "7";
        
            var priceUpdateParam = new PriceUpdateParam
            {
                Channel = channel,
                Store = store,
                Sku = sku,
                VatRate = vatRate,
            };
            var priceApiServiceV2 = testBed.CreateSubject();

            var expectExpDetail = "Invalid payload detected, CDS-Website.10138:CDS-0001 has no prices.";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV2.UpdatePrice(priceUpdateParam, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument
                                                                       && e.Status.Detail == expectExpDetail);
        }
        
        [Fact]
        public async Task UpdatePrice_ParamMissingEveryPrice_ThrowOriginalPriceNoPrice()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
            const string vatRate = "7";
            
            var priceUpdateParam = new PriceUpdateParam
            {
                Channel = channel,
                Store = store,
                Sku = sku,
                VatRate = vatRate,
                OriginalPrice = new PriceDescription { }
            };
            var priceApiServiceV2 = testBed.CreateSubject();

            var expectExpDetail = "Invalid payload detected, OriginalPrice field contains no prices.";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV2.UpdatePrice(priceUpdateParam, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument
                                                                       && e.Status.Detail == expectExpDetail);
        }

        #endregion

        #region UpdatePrices

        [Fact]
        public async Task UpdatePrices_FileUploadDataValid_UploadSuccess()
        {
            //Arrange
            var content = "channel,store,sku,online_price,online_from_date,online_price_enabled,online_to_date,jda_discount_code" + Environment.NewLine;
            content += "CDS-Website,10138,CDS-0001,200,10-01-2020,yes,10-31-2020,jda_disc";
            var chunk = new Chunk
            {
                Content = ByteString.CopyFrom(content, Encoding.Default)
            };
            var chunks = new List<Chunk>() {chunk};
            var requestStream = new ServerStreamMock<Chunk>(chunks);

            var key = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc).ToIsoFormat();

            testBed.Fake<IFileStorageService>()
                .Setup(s => s.Upload(It.IsAny<string>(), It.IsAny<Stream>()))
                .ReturnsAsync(key);
            testBed.Fake<ITopicPublisher>()
                .Setup(s => s.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<TimeSpan>()));
            testBed.Fake<IOptions<MessageBusOption>>()
                .Setup(s => s.Value).Returns(new MessageBusOption
                {
                    WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
                });
            testBed.Fake<IOptions<PriceDefaults>>()
                .Setup(s => s.Value).Returns(new PriceDefaults{ VatRate = 7});

            var priceApiServiceV2 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV2.UpdatePrices(requestStream,
                It.IsAny<ServerCallContext>());

            //Assert
            result.GetType().Should().Be(typeof(Empty));
        }
        
        [Fact]
        public async Task UpdatePrices_FileUploadDataValid_CallFileStorageService()
        {
            //Arrange
            var content = "channel,store,sku,online_price,online_from_date,online_price_enabled,online_to_date,jda_discount_code" + Environment.NewLine;
            content += "CDS-Website,10138,CDS-0001,200,10-01-2020,yes,10-31-2020,jda_disc";
            var chunk = new Chunk
            {
                Content = ByteString.CopyFrom(content, Encoding.Default)
            };
            var chunks = new List<Chunk>() {chunk};
            var requestStream = new ServerStreamMock<Chunk>(chunks);

            var key = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc).ToIsoFormat();
            
            var fileStorageService = new Mock<IFileStorageService>();
            fileStorageService.Setup(s => s.Upload(It.IsAny<string>(), It.IsAny<Stream>()))
                .ReturnsAsync(key);
            testBed.RegisterSingleton(fileStorageService.Object);
            
            testBed.Fake<ITopicPublisher>()
                .Setup(s => s.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<TimeSpan>()));
            testBed.Fake<IOptions<MessageBusOption>>()
                .Setup(s => s.Value).Returns(new MessageBusOption
                {
                    WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
                });
            testBed.Fake<IOptions<PriceDefaults>>()
                .Setup(s => s.Value).Returns(new PriceDefaults{ VatRate = 7});
            
            var priceApiServiceV2 = testBed.CreateSubject();
            
            //Act
            await priceApiServiceV2.UpdatePrices(requestStream, It.IsAny<ServerCallContext>());
            
            //Assert
            fileStorageService.Verify(s => s.Upload(It.IsAny<string>(), It.IsAny<Stream>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePrices_FileUploadFailure_ThrowInvalidArgument()
        {
            //Arrange
            var chunk = new Chunk
            {
                Content = ByteString.CopyFrom("This is the sentence for mock chunk", Encoding.Unicode)
            };
            var chunks = new List<Chunk>() {chunk};
            var requestStream = new ServerStreamMock<Chunk>(chunks);
            
            testBed.Fake<IFileStorageService>()
                .Setup(s => s.Upload(It.IsAny<string>(), It.IsAny<Stream>()))
                .Throws(new InvalidOperationException("Couldn't upload file"));
            var priceApiServiceV2 = testBed.CreateSubject();

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV2.UpdatePrices(requestStream, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument);
        }
        
        [Fact]
        public async Task UpdatePrices_FileUploadDataInvalid_ThrowInvalidArgument()
        {
            //Arrange
            var content = "bu,channel,store,sku,online_price,online_from_date,online_price_enabled,online_to_date,jda_discount_code" + Environment.NewLine;
            content += ",,,,,,,,";
            var chunk = new Chunk
            {
                Content = ByteString.CopyFrom(content, Encoding.Default)
            };
            var chunks = new List<Chunk>() {chunk};
            var requestStream = new ServerStreamMock<Chunk>(chunks);

            var key = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc).ToIsoFormat();

            testBed.Fake<IFileStorageService>()
                .Setup(s => s.Upload(It.IsAny<string>(), It.IsAny<Stream>()))
                .ReturnsAsync(key);
            var priceApiServiceV2 = testBed.CreateSubject();

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV2.UpdatePrices(requestStream, It.IsAny<ServerCallContext>());
            };

            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument);
        }
        #endregion
    }
}