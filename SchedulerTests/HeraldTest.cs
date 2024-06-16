using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Confluent.Kafka;
using CTO.Price.Scheduler.Actors.PriceImport;
using CTO.Price.Shared;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Extensions;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RZ.Foundation;
using RZ.Foundation.Extensions;
using TestUtility;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;
using DateTime = System.DateTime;
using static RZ.Foundation.Prelude;
#pragma warning disable 1998

namespace SchedulerTests
{
    public class HeraldTest : TestKit
    {
        readonly TestBed<PriceService> testBed;
        private readonly ITestOutputHelper testOutputHelper;

        public HeraldTest(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
            testBed = new TestBed<PriceService>(testOutputHelper);
        }
        
        [Fact]
        public async Task BasePriceOfChannelStoreSent_NoException_SendCompleteMessageBackToSender()
        {
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 11, 15, 10, 0, 0);

            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Store = "10138",
                Sku = "123456",
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    Start = new DateTime(2020, 05, 10, 10, 0, 0),
                    End = new DateTime(2020, 10, 10, 10, 0, 0)
                },
                Timestamp = payloadTime
            };

            var priceModel = payload.ToPriceModel(7, payloadTime);

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var messageBus = new MessageBusMock();
            var priceWriter = new Mock<IPriceService>();
            priceWriter.Setup(s => s.GetBaseAndChannelPrice(It.IsAny<PriceModelKey>()))
                .ReturnsAsync(() => new BaseAndChannelPrice(priceModel, new Option<PriceModel>()));

            var performanceCounter = new Mock<IPerformanceCounter>();
            performanceCounter
                .Setup(p => p.CollectPerformance(CodeBlock.UpdatePrice, It.IsAny<Func<Task<(UpdateResult, Option<PriceModel>)>>>()))
                .ReturnsAsync(
                    () => (UpdateResult.Updated, Optional(new PriceModel())));
            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton(performanceCounter.Object);
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton(Mock.Of<ISystemLogService>());
            services.AddSingleton<IMessageBus>(messageBus);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

            //Act
            var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

            // Assert
            result.Should().Be(ActorStatus.Complete.Instance);
        }
        
        [Fact]
        public async Task BasePriceOfChannelStoreSent_MultipleSchedulesInOneMessage_LogSystemErrorAndNotSendCompleteMessage()
        {
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 11, 15, 10, 0, 0);

            var exceptionThrown = new Exception("Unhandled Exception");

            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Store = "10138",
                Sku = "123456",
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    Start = new DateTime(2020, 05, 10, 10, 0, 0),
                    End = new DateTime(2022, 10, 10, 10, 0, 0)
                },
                SalePrice = new RawPriceDescription
                {
                    PriceVat = 50,
                    Start = new DateTime(2020, 05, 10, 10, 0, 0),
                    End = new DateTime(2023, 10, 10, 10, 0, 0)
                },
                Timestamp = payloadTime
            };

            var priceModel = payload.ToPriceModel(7, payloadTime);

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var messageBus = new MessageBusMock();
            var priceWriter = new Mock<IPriceService>();
            priceWriter.Setup(s => s.GetBaseAndChannelPrice(It.IsAny<PriceModelKey>()))
                .ReturnsAsync(() => new BaseAndChannelPrice(priceModel, new Option<PriceModel>()));

            var performanceCounter = new Mock<IPerformanceCounter>();
            performanceCounter
                .Setup(p => p.CollectPerformance(CodeBlock.UpdatePrice,
                    It.IsAny<Func<Task<(UpdateResult, Option<PriceModel>)>>>()))
                .Throws(exceptionThrown);

            var systemLogService = new Mock<ISystemLogService>();
            
            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton(performanceCounter.Object);
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(systemLogService.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton<IMessageBus>(messageBus);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

            //Act
            Func<Task> act = async () =>
            {
                await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());
            };
            await act.Should().ThrowAsync<AskTimeoutException>();

            // Assert
            systemLogService.Verify(s => s.Error(It.Is<AggregateException>(e => e.InnerException == exceptionThrown), It.IsAny<string>()), Times.Between(0, 1, Moq.Range.Inclusive));
        }

        [Fact]
        public async Task BasePriceOfChannelStoreSent_BasePriceRequiresUpdate_PublishesMessageReturnsCompleteMessage()
        {
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 12, 15, 10, 0, 0);

            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Store = "10138",
                Sku = "123456",
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 08, 15, 10, 0, 0)
                },
                Timestamp = payloadTime
            };

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var searchKey = new PriceModelKey(payload.Channel, payload.Store, payload.Sku);

            var basePriceFromStorage = new PriceModel()
            {
                Key = searchKey.GetBaseKey(),
                OriginalPrice = new PriceDescription
                {
                    Vat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };

            var channelPriceFromStorage = new PriceModel()
            {
                Key = searchKey,
                OriginalPrice = new PriceDescription
                {
                    Vat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };

            var messageBus = new MessageBusMock();
            var priceWriter = new Mock<IPriceService>();
            priceWriter
                .Setup(p => p.UpdatePrice(It.IsAny<PriceModel>(), It.IsAny<DateTime>()))
                .ReturnsAsync((UpdateResult.Updated, basePriceFromStorage.ToOption()));
            priceWriter
                .Setup(p => p.GetChannelPrice(It.IsAny<PriceModelKey>()))
                .ReturnsAsync(channelPriceFromStorage);
            priceWriter
                .Setup(p => p.GetChannelKeyToUpdateFromBaseKey(new PriceModelKey(null, "10138", "123456")))
                .Returns((new PriceModelKey("CDS-Website", "10138", "123456")).ToOption());

            var scheduleService = new Mock<IScheduleService>();
            var scheduleStorage = new Mock<IScheduleStorage>();
            var topicPublisher = new Mock<ITopicPublisher>();

            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton<IPerformanceCounter>(new PerformanceCounter());
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(scheduleService.Object);
            services.AddSingleton(scheduleStorage.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton(topicPublisher.Object);

            services.AddSingleton(Mock.Of<ISystemLogService>());
            services.AddSingleton<IMessageBus>(messageBus);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

            //Act
            var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

            // Assert
            result.Should().Be(ActorStatus.Complete.Instance);
            topicPublisher.Verify(
                s => s.Publish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<DeliveryReport<string, string>>>()), Times.Once);
        }

        [Fact]
        public async Task BasePriceOfNonChannelStoreSent_BasePriceRequiresUpdate_NotPublishMessageReturnsCompleteMessage()
        {
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 12, 15, 10, 0, 0);

            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Store = "10140",
                Sku = "123456",
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 08, 15, 10, 0, 0)
                },
                Timestamp = payloadTime
            };

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var searchKey = new PriceModelKey(payload.Channel, payload.Store, payload.Sku);

            var basePriceFromStorage = new PriceModel()
            {
                Key = searchKey.GetBaseKey(),
                OriginalPrice = new PriceDescription
                {
                    Vat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };

            var channelPriceFromStorage = new PriceModel()
            {
                Key = searchKey,
                OriginalPrice = new PriceDescription
                {
                    Vat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };

            var messageBus = new MessageBusMock();
            var priceWriter = new Mock<IPriceService>();
            priceWriter
                .Setup(p => p.UpdatePrice(It.IsAny<PriceModel>(), It.IsAny<DateTime>()))
                .ReturnsAsync((UpdateResult.Updated, basePriceFromStorage.ToOption()));
            priceWriter
                .Setup(p => p.GetChannelPrice(It.IsAny<PriceModelKey>()))
                .ReturnsAsync(channelPriceFromStorage);
            priceWriter
                .Setup(p => p.GetChannelKeyToUpdateFromBaseKey(new PriceModelKey(null, "10138", "123456")))
                .Returns((new PriceModelKey("CDS-Website", "10138", "123456")).ToOption());

            var scheduleService = new Mock<IScheduleService>();
            var scheduleStorage = new Mock<IScheduleStorage>();
            var topicPublisher = new Mock<ITopicPublisher>();

            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton<IPerformanceCounter>(new PerformanceCounter());
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(scheduleService.Object);
            services.AddSingleton(scheduleStorage.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton(topicPublisher.Object);

            services.AddSingleton(Mock.Of<ISystemLogService>());
            services.AddSingleton<IMessageBus>(messageBus);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

            //Act
            var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

            // Assert
            result.Should().Be(ActorStatus.Complete.Instance);
            topicPublisher.Verify(s => s.Publish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<DeliveryReport<string, string>>>()), Times.Never);
        }

        [Fact]
        public async Task BasePriceOfChannelStoreSent_RequiresScheduling_SendCompleteMessageBackToSender()
        {
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 11, 15, 10, 0, 0);
            
            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });

            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Store = "10138",
                Sku = "123456",
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                },
                Timestamp = payloadTime
            };
            
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var searchKey = new PriceModelKey(payload.Channel, payload.Store, payload.Sku);
            var basePriceFromStorage = new PriceModel()
            {
                Key = searchKey.GetBaseKey(),
                OriginalPrice = new PriceDescription
                {
                    Vat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };
            var channelPriceFromStorage = new PriceModel()
            {
                Key = searchKey,
                OriginalPrice = new PriceDescription
                {
                    Vat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };
            
            var messageBus = new MessageBusMock();
            var priceWriter = new Mock<IPriceService>();

            priceWriter
                .Setup(p => p.GetChannelKeyToUpdateFromBaseKey(new PriceModelKey(null, "10138", "123456")))
                .Returns((new PriceModelKey("CDS-Website", "10138", "123456")).ToOption());

            priceWriter
                .Setup(p => p.UpdatePrice(It.IsAny<PriceModel>(), It.IsAny<DateTime>()))
                .ReturnsAsync((UpdateResult.Created, basePriceFromStorage));

            var scheduleService = new Mock<IScheduleService>();
            var scheduleStorage = new Mock<IScheduleStorage>();
            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton<IPerformanceCounter>(new PerformanceCounter());
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(scheduleService.Object);
            services.AddSingleton(scheduleStorage.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton(Mock.Of<ITopicPublisher>());

            services.AddSingleton(Mock.Of<ISystemLogService>());
            services.AddSingleton<IMessageBus>(messageBus);

            
            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

            //Act
            var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

            // Assert
            result.Should().Be(ActorStatus.Complete.Instance);
        }
        
        [Fact]
        public async Task BasePriceOfChannelStoreSent_PriceInStorageDoesntExistAndUpdateIsExpired_SendCompleteMessageBackToSender()
        {
            {
                //Arrange
                var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
                var processTime = new DateTime(2020, 05, 15, 10, 0, 0);

                var payload = new RawPrice
                {
                    Version = "price.v2",
                    Event = "raw.price",
                    Store = "10138",
                    Sku = "123456",
                    VatRate = 7,
                    OriginalPrice = new RawPriceDescription
                    {
                        PriceVat = 100,
                        End = payloadTime
                    },
                    SalePrice = null,
                    PromotionPrice = null,
                    Timestamp = payloadTime
                };

                var actorEngineStartup = new Mock<IActorEngineStartup>();

                var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
                {
                    WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
                });
                var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

                var messageBus = new MessageBusMock();
                var priceWriter = new Mock<IPriceService>();
                
                priceWriter
                    .Setup(p => p.GetChannelKeyToUpdateFromBaseKey(new PriceModelKey(null, "10138", "123456")))
                    .Returns((new PriceModelKey("CDS-Website", "10138", "123456")).ToOption());

                priceWriter
                    .Setup(p => p.UpdatePrice(It.IsAny<PriceModel>(), It.IsAny<DateTime>()))
                    .ReturnsAsync((UpdateResult.Ignored, None<PriceModel>()));
                
                var scheduleService = new Mock<IScheduleService>();
                var scheduleStorage = new Mock<IScheduleStorage>();
                var services = new ServiceCollection();
                var dateTimeProvider = new Mock<IDateTimeProvider>();
                var performanceCounter = new PerformanceCounter();
                dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

                services.AddSingleton(messageBusMonitor.Object);
                services.AddSingleton(priceDefaultMonitor.Object);
                services.AddSingleton(priceWriter.Object);
                services.AddSingleton(scheduleService.Object);
                services.AddSingleton(scheduleStorage.Object);
                services.AddSingleton(dateTimeProvider.Object);
                services.AddSingleton(actorEngineStartup.Object);
                services.AddSingleton(Mock.Of<IEventLogService>());
                services.AddSingleton<IPerformanceCounter>(performanceCounter);

                services.AddSingleton(Mock.Of<ISystemLogService>());
                services.AddSingleton<IMessageBus>(messageBus);

                Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

                var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

                //Act
                var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

                // Assert
                result.Should().Be(ActorStatus.Complete.Instance);
                performanceCounter.IgnoredCounter.Should().Be(1);
            }
        }
        
        [Fact]
        public async Task BasePriceOfChannelStoreSent_PriceInStorageExistAndUpdateApplied_PublisherIsCalledSendCompleteMessageBackToSender()
        {
            {
                //Arrange
                var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
                var processTime = new DateTime(2020, 05, 15, 10, 0, 0);
                var updateKey = new PriceModelKey(null, "10138", "123456");

                var payload = new RawPrice
                {
                    Version = "price.v2",
                    Event = "raw.price",
                    Store = updateKey.Store,
                    Sku = updateKey.Sku,
                    VatRate = 7,
                    OriginalPrice = new RawPriceDescription
                    {
                        PriceVat = 100,
                        End = payloadTime
                    },
                    SalePrice = null,
                    PromotionPrice = null,
                    Timestamp = payloadTime
                };
                
                var basePriceFromStorage = new PriceModel()
                {
                    Key = updateKey,
                    OriginalPrice = new PriceDescription
                    {
                        Vat = 100,
                        Start = new DateTime(2020, 05, 15, 10, 0, 0),
                        End = new DateTime(2020, 12, 15, 10, 0, 0)
                    }
                };

                var actorEngineStartup = new Mock<IActorEngineStartup>();

                var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
                {
                    WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
                });
                var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

                var messageBus = new MessageBusMock();
                var priceWriter = new Mock<IPriceService>();
                
                priceWriter
                    .Setup(p => p.GetChannelKeyToUpdateFromBaseKey(new PriceModelKey(null, "10138", "123456")))
                    .Returns((new PriceModelKey("CDS-Website", "10138", "123456")).ToOption());

                priceWriter
                    .Setup(p => p.UpdatePrice(It.IsAny<PriceModel>(), It.IsAny<DateTime>()))
                    .ReturnsAsync((UpdateResult.Updated, basePriceFromStorage));
                
                var scheduleService = new Mock<IScheduleService>();
                var scheduleStorage = new Mock<IScheduleStorage>();
                var services = new ServiceCollection();
                var dateTimeProvider = new Mock<IDateTimeProvider>();
                var publisher = new Mock<ITopicPublisher>();
                var performanceCounter = new PerformanceCounter();
                dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

                services.AddSingleton(messageBusMonitor.Object);
                services.AddSingleton(priceDefaultMonitor.Object);
                services.AddSingleton(priceWriter.Object);
                services.AddSingleton(scheduleService.Object);
                services.AddSingleton(scheduleStorage.Object);
                services.AddSingleton(dateTimeProvider.Object);
                services.AddSingleton(actorEngineStartup.Object);
                services.AddSingleton(publisher.Object);
                services.AddSingleton(Mock.Of<IEventLogService>());
                services.AddSingleton<IPerformanceCounter>(performanceCounter);

                services.AddSingleton(Mock.Of<ISystemLogService>());
                services.AddSingleton<IMessageBus>(messageBus);

                Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

                var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

                //Act
                var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

                // Assert
                result.Should().Be(ActorStatus.Complete.Instance);
                publisher.Verify(s => s.Publish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<DeliveryReport<string, string>>>()));
            }
        }
        
        [Fact]
        public async Task BasePriceOfChannelStoreSent_PriceInStorageExpires_PublisherIsCalledSendCompleteMessageBackToSender()
        {
            
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 05, 15, 10, 0, 0);
            var updateKey = new PriceModelKey(null, "10138", "123456");

            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Store = updateKey.Store,
                Sku = updateKey.Sku,
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    End = payloadTime
                },
                SalePrice = null,
                PromotionPrice = null,
                Timestamp = payloadTime
            };
            
            var basePriceFromStorage = new PriceModel()
            {
                Key = updateKey,
                OriginalPrice = new PriceDescription
                {
                    Vat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var messageBus = new MessageBusMock();
            var priceWriter = new Mock<IPriceService>();
            
            priceWriter
                .Setup(p => p.GetChannelKeyToUpdateFromBaseKey(new PriceModelKey(null, "10138", "123456")))
                .Returns((new PriceModelKey("CDS-Website", "10138", "123456")).ToOption());

            priceWriter
                .Setup(p => p.UpdatePrice(It.IsAny<PriceModel>(), It.IsAny<DateTime>()))
                .ReturnsAsync((UpdateResult.Deleted, basePriceFromStorage));
            
            var scheduleService = new Mock<IScheduleService>();
            var scheduleStorage = new Mock<IScheduleStorage>();
            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            var publisher = new Mock<ITopicPublisher>();
            var performanceCounter = new PerformanceCounter();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(scheduleService.Object);
            services.AddSingleton(scheduleStorage.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(publisher.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton<IPerformanceCounter>(performanceCounter);

            services.AddSingleton(Mock.Of<ISystemLogService>());
            services.AddSingleton<IMessageBus>(messageBus);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

            //Act
            var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

            // Assert
            result.Should().Be(ActorStatus.Complete.Instance);
            publisher.Verify(s => s.Publish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<DeliveryReport<string, string>>>()), Times.Never);
            performanceCounter.IgnoredCounter.Should().Be(1);
        }
        
        [Fact]
        public async Task ActiveChannelPriceSent_BasePriceAlreadyExistsInStorageChannelPriceDoesntAlreadyExistInStorage_PublisherIsCalledAndSendCompleteMessageBackToSender()
        {
            
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 05, 15, 10, 0, 0);
            var updateKey = new PriceModelKey("CDS-Website", "10138", "123456");

            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Channel = updateKey.Channel,
                Store = updateKey.Store,
                Sku = updateKey.Sku,
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    End = payloadTime
                },
                SalePrice = null,
                PromotionPrice = null,
                Timestamp = payloadTime
            };
            
            var basePriceFromStorage = new PriceModel()
            {
                Key = updateKey.GetBaseKey(),
                OriginalPrice = new PriceDescription
                {
                    Vat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };
            
            var channelPriceFromStorage = new PriceModel()
            {
                Key = updateKey.GetBaseKey(),
                OriginalPrice = new PriceDescription
                {
                    Vat = 50,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var messageBus = new MessageBusMock();
            var priceWriter = new Mock<IPriceService>();
            
            priceWriter
                .Setup(p => p.GetChannelKeyToUpdateFromBaseKey(new PriceModelKey(null, "10138", "123456")))
                .Returns((new PriceModelKey("CDS-Website", "10138", "123456")).ToOption());
            
            priceWriter
                .Setup(p => p.GetBasePrice(updateKey))
                .ReturnsAsync(basePriceFromStorage.ToOption());

            priceWriter
                .Setup(p => p.UpdatePrice(It.IsAny<PriceModel>(), It.IsAny<DateTime>()))
                .ReturnsAsync((UpdateResult.Created, channelPriceFromStorage));
            
            var scheduleService = new Mock<IScheduleService>();
            var scheduleStorage = new Mock<IScheduleStorage>();
            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            var publisher = new Mock<ITopicPublisher>();
            var performanceCounter = new PerformanceCounter();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(scheduleService.Object);
            services.AddSingleton(scheduleStorage.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(publisher.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton<IPerformanceCounter>(performanceCounter);

            services.AddSingleton(Mock.Of<ISystemLogService>());
            services.AddSingleton<IMessageBus>(messageBus);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

            //Act
            var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

            // Assert
            result.Should().Be(ActorStatus.Complete.Instance);
            publisher.Verify(s => s.Publish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<DeliveryReport<string, string>>>()), Times.Once);
            performanceCounter.IgnoredCounter.Should().Be(0);
        }
        
        [Fact]
        public async Task ActiveChannelPriceSent_BasePriceDoesntAlreadyExistsInStorageChannelPriceDoesntAlreadyExistInStorage_PublisherIsCalledAndSendCompleteMessageBackToSender()
        {
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 05, 15, 10, 0, 0);
            var updateKey = new PriceModelKey("CDS-Website", "10138", "123456");

            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Channel = updateKey.Channel,
                Store = updateKey.Store,
                Sku = updateKey.Sku,
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    End = payloadTime
                },
                SalePrice = null,
                PromotionPrice = null,
                Timestamp = payloadTime
            };
            
            var basePriceFromStorage = new PriceModel()
            {
                Key = updateKey.GetBaseKey(),
                OriginalPrice = new PriceDescription
                {
                    Vat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };
            
            var channelPriceFromStorage = new PriceModel()
            {
                Key = updateKey.GetBaseKey(),
                OriginalPrice = new PriceDescription
                {
                    Vat = 50,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var messageBus = new MessageBusMock();
            var priceWriter = new Mock<IPriceService>();
            
            priceWriter
                .Setup(p => p.GetChannelKeyToUpdateFromBaseKey(new PriceModelKey(null, "10138", "123456")))
                .Returns((new PriceModelKey("CDS-Website", "10138", "123456")).ToOption());
            
            priceWriter
                .Setup(p => p.GetBasePrice(updateKey))
                .ReturnsAsync(None<PriceModel>());

            priceWriter
                .Setup(p => p.UpdatePrice(It.IsAny<PriceModel>(), It.IsAny<DateTime>()))
                .ReturnsAsync((UpdateResult.Created, channelPriceFromStorage));
            
            var scheduleService = new Mock<IScheduleService>();
            var scheduleStorage = new Mock<IScheduleStorage>();
            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            var publisher = new Mock<ITopicPublisher>();
            var performanceCounter = new PerformanceCounter();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(scheduleService.Object);
            services.AddSingleton(scheduleStorage.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(publisher.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton<IPerformanceCounter>(performanceCounter);

            services.AddSingleton(Mock.Of<ISystemLogService>());
            services.AddSingleton<IMessageBus>(messageBus);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

            //Act
            var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

            // Assert
            result.Should().Be(ActorStatus.Complete.Instance);
            publisher.Verify(s => s.Publish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<DeliveryReport<string, string>>>()), Times.Never);
            performanceCounter.IgnoredCounter.Should().Be(1);
        }
        
        [Fact]
        public async Task ActiveChannelPriceSent_BasePriceAlreadyExistsInStorageChannelPriceAlreadyExistInStorage_PublisherIsCalledAndSendCompleteMessageBackToSender()
        {
            
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 05, 15, 10, 0, 0);
            var updateKey = new PriceModelKey("CDS-Website", "10138", "123456");

            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Channel = updateKey.Channel,
                Store = updateKey.Store,
                Sku = updateKey.Sku,
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    End = payloadTime
                },
                SalePrice = null,
                PromotionPrice = null,
                Timestamp = payloadTime
            };
            
            var basePriceFromStorage = new PriceModel()
            {
                Key = updateKey.GetBaseKey(),
                OriginalPrice = new PriceDescription
                {
                    Vat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };
            
            var channelPriceFromStorage = new PriceModel()
            {
                Key = updateKey.GetBaseKey(),
                OriginalPrice = new PriceDescription
                {
                    Vat = 50,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var messageBus = new MessageBusMock();
            var priceWriter = new Mock<IPriceService>();
            
            priceWriter
                .Setup(p => p.GetChannelKeyToUpdateFromBaseKey(new PriceModelKey(null, "10138", "123456")))
                .Returns((new PriceModelKey("CDS-Website", "10138", "123456")).ToOption());
            
            priceWriter
                .Setup(p => p.GetBasePrice(updateKey))
                .ReturnsAsync(basePriceFromStorage.ToOption());

            priceWriter
                .Setup(p => p.UpdatePrice(It.IsAny<PriceModel>(), It.IsAny<DateTime>()))
                .ReturnsAsync((UpdateResult.Updated, channelPriceFromStorage));
            
            var scheduleService = new Mock<IScheduleService>();
            var scheduleStorage = new Mock<IScheduleStorage>();
            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            var publisher = new Mock<ITopicPublisher>();
            var performanceCounter = new PerformanceCounter();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(scheduleService.Object);
            services.AddSingleton(scheduleStorage.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(publisher.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton<IPerformanceCounter>(performanceCounter);

            services.AddSingleton(Mock.Of<ISystemLogService>());
            services.AddSingleton<IMessageBus>(messageBus);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

            //Act
            var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

            // Assert
            result.Should().Be(ActorStatus.Complete.Instance);
            publisher.Verify(s => s.Publish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<DeliveryReport<string, string>>>()), Times.Once);
            performanceCounter.IgnoredCounter.Should().Be(0);
        }
        
        [Fact]
        public async Task PastChannelPriceSent_BasePriceAlreadyExistsInStorageChannelPriceAlreadyExistInStorage_PublisherIsCalledAndSendCompleteMessageBackToSender()
        {
            
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 05, 15, 10, 0, 0);
            var updateKey = new PriceModelKey("CDS-Website", "10138", "123456");

            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Channel = updateKey.Channel,
                Store = updateKey.Store,
                Sku = updateKey.Sku,
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    End = payloadTime
                },
                SalePrice = null,
                PromotionPrice = null,
                Timestamp = payloadTime
            };
            
            var basePriceFromStorage = new PriceModel()
            {
                Key = updateKey.GetBaseKey(),
                OriginalPrice = new PriceDescription
                {
                    Vat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var messageBus = new MessageBusMock();
            var priceWriter = new Mock<IPriceService>();
            
            priceWriter
                .Setup(p => p.GetChannelKeyToUpdateFromBaseKey(new PriceModelKey(null, "10138", "123456")))
                .Returns((new PriceModelKey("CDS-Website", "10138", "123456")).ToOption());
            
            priceWriter
                .Setup(p => p.GetBasePrice(updateKey))
                .ReturnsAsync(basePriceFromStorage.ToOption());

            priceWriter
                .Setup(p => p.UpdatePrice(It.IsAny<PriceModel>(), It.IsAny<DateTime>()))
                .ReturnsAsync((UpdateResult.Ignored, None<PriceModel>()));
            
            var scheduleService = new Mock<IScheduleService>();
            var scheduleStorage = new Mock<IScheduleStorage>();
            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            var publisher = new Mock<ITopicPublisher>();
            var performanceCounter = new PerformanceCounter();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(scheduleService.Object);
            services.AddSingleton(scheduleStorage.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(publisher.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton<IPerformanceCounter>(performanceCounter);

            services.AddSingleton(Mock.Of<ISystemLogService>());
            services.AddSingleton<IMessageBus>(messageBus);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

            //Act
            var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

            // Assert
            result.Should().Be(ActorStatus.Complete.Instance);
            publisher.Verify(s => s.Publish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<DeliveryReport<string, string>>>()), Times.Never);
            performanceCounter.IgnoredCounter.Should().Be(1);
        }
        
        [Fact]
        public async Task ExpiredChannelPriceSent_BasePriceAlreadyExistsInStorageChannelPriceAlreadyExistInStorage_PublisherIsCalledAndSendCompleteMessageBackToSender()
        {
            
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 05, 15, 10, 0, 0);
            var updateKey = new PriceModelKey("CDS-Website", "10138", "123456");

            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Channel = updateKey.Channel,
                Store = updateKey.Store,
                Sku = updateKey.Sku,
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    End = payloadTime
                },
                SalePrice = null,
                PromotionPrice = null,
                Timestamp = payloadTime
            };
            
            var basePriceFromStorage = new PriceModel()
            {
                Key = updateKey.GetBaseKey(),
                OriginalPrice = new PriceDescription
                {
                    Vat = 100,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };
            
            var channelPriceFromStorage = new PriceModel()
            {
                Key = updateKey.GetBaseKey(),
                OriginalPrice = new PriceDescription
                {
                    Vat = 50,
                    Start = new DateTime(2020, 05, 15, 10, 0, 0),
                    End = new DateTime(2020, 12, 15, 10, 0, 0)
                }
            };

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var messageBus = new MessageBusMock();
            var priceWriter = new Mock<IPriceService>();
            
            priceWriter
                .Setup(p => p.GetChannelKeyToUpdateFromBaseKey(new PriceModelKey(null, "10138", "123456")))
                .Returns((new PriceModelKey("CDS-Website", "10138", "123456")).ToOption());
            
            priceWriter
                .Setup(p => p.GetBasePrice(updateKey))
                .ReturnsAsync(basePriceFromStorage.ToOption());

            priceWriter
                .Setup(p => p.UpdatePrice(It.IsAny<PriceModel>(), It.IsAny<DateTime>()))
                .ReturnsAsync((UpdateResult.Deleted, channelPriceFromStorage));
            
            var scheduleService = new Mock<IScheduleService>();
            var scheduleStorage = new Mock<IScheduleStorage>();
            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            var publisher = new Mock<ITopicPublisher>();
            var performanceCounter = new PerformanceCounter();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(scheduleService.Object);
            services.AddSingleton(scheduleStorage.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(publisher.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton<IPerformanceCounter>(performanceCounter);

            services.AddSingleton(Mock.Of<ISystemLogService>());
            services.AddSingleton<IMessageBus>(messageBus);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

            //Act
            var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

            // Assert
            result.Should().Be(ActorStatus.Complete.Instance);
            publisher.Verify(s => s.Publish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<DeliveryReport<string, string>>>()), Times.Once);
            performanceCounter.IgnoredCounter.Should().Be(0);
        }


        [Fact]
        public async Task BasePriceWithNoPrices_HeraldReceivedSuchMessage_SendCompleteMessageBackToSender()
        {
            {
                //Arrange
                var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
                var processTime = new DateTime(2020, 05, 15, 10, 0, 0);

                var payload = new RawPrice
                {
                    Version = "price.v2",
                    Event = "raw.price",
                    Store = "10138",
                    Sku = "123456",
                    VatRate = 7,
                    OriginalPrice = null,
                    SalePrice = null,
                    PromotionPrice = null,
                    Timestamp = payloadTime
                };

                var actorEngineStartup = new Mock<IActorEngineStartup>();

                var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
                {
                    WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
                });
                var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

                var messageBus = new MessageBusMock();
                var priceWriter = new Mock<IPriceService>();
                var scheduleService = new Mock<IScheduleService>();
                var scheduleStorage = new Mock<IScheduleStorage>();
                var performanceCounter = new Mock<IPerformanceCounter>();
                var services = new ServiceCollection();
                var dateTimeProvider = new Mock<IDateTimeProvider>();
                dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

                services.AddSingleton(messageBusMonitor.Object);
                services.AddSingleton(priceDefaultMonitor.Object);
                services.AddSingleton(performanceCounter.Object);
                services.AddSingleton(priceWriter.Object);
                services.AddSingleton(scheduleService.Object);
                services.AddSingleton(scheduleStorage.Object);
                services.AddSingleton(dateTimeProvider.Object);
                services.AddSingleton(actorEngineStartup.Object);
                services.AddSingleton(Mock.Of<IEventLogService>());
                services.AddSingleton<IPerformanceCounter>(new PerformanceCounter());

                services.AddSingleton(Mock.Of<ISystemLogService>());
                services.AddSingleton<IMessageBus>(messageBus);

                Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

                var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));

                //Act
                var result = await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());

                // Assert
                result.Should().Be(ActorStatus.Complete.Instance);
            }
        }
        
        [Fact]
        public async Task ScheduledMessageSent_UnexpectedExceptionThrown_SendCompleteMessageBackToSender()
        {
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 11, 15, 10, 0, 0);
            
            var exceptionThrown = new Exception("Unhandled Exception");
            
            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Store = "10138",
                Sku = "123456",
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    Start = new DateTime(2020, 05, 10, 10, 0, 0),
                    End = new DateTime(2020, 10, 10, 10, 0, 0)
                },
                Timestamp = payloadTime
            };

            var priceModel = payload.ToPriceModel(7, payloadTime);

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var messageBus = new MessageBusMock();
            
            var systemLogService = new Mock<ISystemLogService>();
            
            var priceWriter = new Mock<IPriceService>();
            priceWriter.Setup(s => s.GetBaseAndChannelPrice(It.IsAny<PriceModelKey>()))
                .ReturnsAsync(() => new BaseAndChannelPrice(priceModel, new Option<PriceModel>()));

            var performanceCounter = new Mock<IPerformanceCounter>();
            performanceCounter
                .Setup(p => p.CollectPerformance(CodeBlock.UpdatePrice,
                    It.IsAny<Func<Task<(UpdateResult, Option<PriceModel>)>>>()))
                .Throws(exceptionThrown);
            
            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton(performanceCounter.Object);
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton(systemLogService.Object);
            services.AddSingleton<IMessageBus>(messageBus);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));
            
            //Act
            Func<Task> act = async () =>
            {
                await herald.Ask<ActorStatus.Complete>(payload, 5.Seconds());
            };
            await act.Should().ThrowAsync<AskTimeoutException>();

            // Assert
            systemLogService.Verify(s => s.Error(It.Is<AggregateException>(e => e.InnerException == exceptionThrown), It.IsAny<string>()), Times.Between(0, 1, Moq.Range.Inclusive));
        }
        
        [Fact]
        public async Task Scheduled_NoException_SendCompleteMessageBackToSender()
        {
            //Arrange
            var payloadTime = new DateTime(2020, 05, 15, 9, 0, 0);
            var processTime = new DateTime(2020, 11, 15, 10, 0, 0);

            var payload = new RawPrice
            {
                Version = "price.v2",
                Event = "raw.price",
                Store = "10138",
                Sku = "123456",
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = 100,
                    Start = new DateTime(2020, 05, 10, 10, 0, 0),
                    End = new DateTime(2020, 10, 10, 10, 0, 0)
                },
                Timestamp = payloadTime
            };

            var priceModel = payload.ToPriceModel(7, payloadTime);

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var priceDefaultMonitor = MockUtils.MockOption(new PriceDefaults {VatRate = 7});

            var messageBus = new MessageBusMock();
            var priceWriter = new Mock<IPriceService>();
            priceWriter.Setup(s => s.GetBaseAndChannelPrice(It.IsAny<PriceModelKey>()))
                .ReturnsAsync(() => new BaseAndChannelPrice(priceModel, new Option<PriceModel>()));
            
            var systemLogService = new Mock<ISystemLogService>();

            var performanceCounter = new Mock<IPerformanceCounter>();
            performanceCounter
                .Setup(p => p.CollectPerformance(CodeBlock.UpdatePrice, It.IsAny<Func<Task<(UpdateResult, Option<PriceModel>)>>>()))
                .ReturnsAsync(
                    () => (UpdateResult.Updated, Optional(new PriceModel())));
            var services = new ServiceCollection();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(dp => dp.UtcNow()).Returns(() => processTime);

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton(priceDefaultMonitor.Object);
            services.AddSingleton(performanceCounter.Object);
            services.AddSingleton(priceWriter.Object);
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton(systemLogService.Object);
            services.AddSingleton<IMessageBus>(messageBus);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            var herald = Sys.ActorOf(Props.Create(() => new PriceHerald()));
            
            //Act
            var result = await herald.Ask<ActorStatus.Complete>(payload.ToPriceModel(7, DateTime.UtcNow), 5.Seconds());

            // Assert
            result.Should().Be(ActorStatus.Complete.Instance);
        }
    }
}