using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using CTO.Price.Scheduler.Actors.PriceImport;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RZ.Foundation.Extensions;
using TestUtility;
using Xunit;
using DateTime = System.DateTime;
using JsonConvert = Newtonsoft.Json.JsonConvert;


namespace SchedulerTests
{
    public class IncomingRawPriceTest : TestKit
    {
        public IncomingRawPriceTest() : base(@"akka.scheduler.implementation = ""Akka.TestKit.TestScheduler, Akka.TestKit""") { }
        private TestScheduler Scheduler => (TestScheduler)Sys.Scheduler;
        
        [Fact]
        public async Task IncomingRawPrice_CorrectPayload_ShouldTellMessage() {
            //Arrange
            var probe = CreateTestProbe();
            
            probe.SetAutoPilot(new DelegateAutoPilot((sender, message) =>
                {
                    if(message is GetRoutees)
                        sender.Tell(new Routees(new []{new Routee()}));
                    return AutoPilot.KeepRunning;
                })
            );
            
            var payload = new RawPrice
            {
                Version = "price.v1",
                Event = "price.raw",
                Store = "10138",
                Sku = "123456",
                VatRate = 7,
                OriginalPrice = new RawPriceDescription{ PriceVat = 100 },
                Timestamp = new DateTime(2020,05, 25),
            };

            var actorEngineStartup = new Mock<IActorEngineStartup>();
            
            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var messageBus = new MessageBusMock();
            //Mock queue data
            await messageBus.PublishAsync("IMPORT-TOPIC", "IMPORT-KEY", JsonConvert.SerializeObject(payload), 5.Seconds());
            
            var services = new ServiceCollection();

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton<IMessageBus>(messageBus);
            services.AddSingleton(Mock.Of<IPerformanceCounter>());
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(Mock.Of<ISystemLogService>());
            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            //Act
            //Upon creation should pull message from queue
            var import = Sys.ActorOf(Props.Create(() => new PriceImporter(probe)));

            // Assert
            await AwaitConditionAsync(() => import.Ask(ActorCommand.ReplyIfReady.Instance).Result == ActorStatus.Ready.Instance);
            Scheduler.Advance(20.Seconds());
            probe.ExpectMsg<GetRoutees>();
            var rawPrice = probe.ExpectMsg<RawPrice>();
            rawPrice.Should().BeEquivalentTo(payload);
        }

        [Fact]
        public async Task PriceImporterIsReadyToConsumeMessage_IncorrectPayloadVersion_ShouldNotTellMessageAndGetJsonExceptionMessage() {
            //Arrange
            var probe = CreateTestProbe();
            
            probe.SetAutoPilot(new DelegateAutoPilot((sender, message) =>
                {
                    if(message is GetRoutees)
                        sender.Tell(new Routees(new []{new Routee()}));
                    return AutoPilot.KeepRunning;
                })
            );
            
            var payload = new RawPrice
            {
                Version = "price.test",
                Event = "price.raw",
                Store = "10138",
                Sku = "123456",
                VatRate = 7,
                OriginalPrice = new RawPriceDescription{ PriceVat = 100 },
                Timestamp = new DateTime(2020, 05, 25)
            };
            
            var actorEngineStartup = new Mock<IActorEngineStartup>();
            
            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var messageBus = new MessageBusMock();
            //Mock queue data
            await messageBus.PublishAsync("IMPORT-TOPIC", "IMPORT-KEY", JsonConvert.SerializeObject(payload), 5.Seconds());
            
            var systemLogService = new Mock<ISystemLogService>();
            
            var services = new ServiceCollection();

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton<IMessageBus>(messageBus);
            services.AddSingleton(Mock.Of<IPerformanceCounter>());
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton(systemLogService.Object);
            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            
            //Act
            //Upon creation should pull message from queue
            var import = Sys.ActorOf(Props.Create(() => new PriceImporter(probe)));
            
            await AwaitConditionAsync(() => import.Ask(ActorCommand.ReplyIfReady.Instance).Result == ActorStatus.Ready.Instance);
            Scheduler.Advance(20.Seconds());
            probe.ExpectMsg<GetRoutees>();
            probe.ExpectNoMsg();
        }
        
        [Fact]
        public async Task PriceImporterIsReadyToConsumeMessage_IncorrectPayloadEvent_ShouldNotTellMessageAndGetJsonExceptionMessage() {
            //Arrange
            var probe = CreateTestProbe();
            
            probe.SetAutoPilot(new DelegateAutoPilot((sender, message) =>
                {
                    if(message is GetRoutees)
                        sender.Tell(new Routees(new []{new Routee()}));
                    return AutoPilot.KeepRunning;
                })
            );
            
            var payload = new RawPrice
            {
                Version = "price.v1",
                Event = "testEvent",
                Store = "10138",
                Sku = "123456",
                VatRate = 7,
                OriginalPrice = new RawPriceDescription{ PriceVat = 100 },
                Timestamp = new DateTime(2020, 05, 25)
            };
            
            var actorEngineStartup = new Mock<IActorEngineStartup>();
            
            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var messageBus = new MessageBusMock();
            //Mock queue data
            await messageBus.PublishAsync("IMPORT-TOPIC", "IMPORT-KEY", JsonConvert.SerializeObject(payload), 5.Seconds());
            
            var systemLogService = new Mock<ISystemLogService>();
            
            var services = new ServiceCollection();

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton<IMessageBus>(messageBus);
            services.AddSingleton(Mock.Of<IPerformanceCounter>());
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton(systemLogService.Object);
            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            
            //Act
            //Upon creation should pull message from queue
            var import = Sys.ActorOf(Props.Create(() => new PriceImporter(probe)));
            
            await AwaitConditionAsync(() => import.Ask(ActorCommand.ReplyIfReady.Instance).Result == ActorStatus.Ready.Instance);
            Scheduler.Advance(20.Seconds());
            probe.ExpectMsg<GetRoutees>();
            probe.ExpectNoMsg();
        }
        
        [Fact]
        public async Task IncomingRawPrice_InvalidPriceDetected_ShouldNotTellMessage() {
            //Arrange
            var probe = CreateTestProbe();
            
            probe.SetAutoPilot(new DelegateAutoPilot((sender, message) =>
                {
                    if(message is GetRoutees)
                        sender.Tell(new Routees(new []{new Routee()}));
                    return AutoPilot.KeepRunning;
                })
            );
            
            CreateTestProbe();
            var payload = new RawPrice
            {
                Version = "price.vx",
                Event = "price transform",
                Store = "10138",
                Sku = "123456",
                VatRate = 7,
                OriginalPrice = new RawPriceDescription{ PriceVat = 100 },
                Timestamp = new DateTime(2020,05, 25),
            };

            var actorEngineStartup = new Mock<IActorEngineStartup>();

            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var messageBus = new MessageBusMock();
            //Mock queue data
            await messageBus.PublishAsync("IMPORT-TOPIC", "IMPORT-KEY", JsonConvert.SerializeObject(payload), 5.Seconds());
            
            var systemLogService = new Mock<ISystemLogService>();
            
            var services = new ServiceCollection();

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton<IMessageBus>(messageBus);
            services.AddSingleton(Mock.Of<IPerformanceCounter>());
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(systemLogService.Object);
            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            //Act
            //Upon creation should pull message from queue
            var import = Sys.ActorOf(Props.Create(() => new PriceImporter(probe)));

            // Assert

            await AwaitConditionAsync(() => import.Ask(ActorCommand.ReplyIfReady.Instance).Result == ActorStatus.Ready.Instance);
            Scheduler.Advance(20.Seconds());
            probe.ExpectMsg<GetRoutees>();
            probe.ExpectNoMsg();
        }
        
        [Fact]
        public async Task PriceImporterIsReadyToConsumeMessage_UnableToSerializePayload_ShouldNotTellMessageAndGetJsonExceptionMessage() {
            //Arrange
            var probe = CreateTestProbe();
            
            probe.SetAutoPilot(new DelegateAutoPilot((sender, message) =>
                {
                    if(message is GetRoutees)
                        sender.Tell(new Routees(new []{new Routee()}));
                    return AutoPilot.KeepRunning;
                })
            );
            
            var actorEngineStartup = new Mock<IActorEngineStartup>();
            
            var messageBusMonitor = MockUtils.MockOption(new MessageBusOption
            {
                WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
            });
            var messageBus = new MessageBusMock();
            //Mock queue data
            await messageBus.PublishAsync("IMPORT-TOPIC", "IMPORT-KEY", "invalid payload", 5.Seconds());
            
            var systemLogService = new Mock<ISystemLogService>();
            
            var services = new ServiceCollection();

            services.AddSingleton(messageBusMonitor.Object);
            services.AddSingleton<IMessageBus>(messageBus);
            services.AddSingleton(Mock.Of<IPerformanceCounter>());
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton(systemLogService.Object);
            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            
            //Act
            //Upon creation should pull message from queue
            var import = Sys.ActorOf(Props.Create(() => new PriceImporter(probe)));
            
            await AwaitConditionAsync(() => import.Ask(ActorCommand.ReplyIfReady.Instance).Result == ActorStatus.Ready.Instance);
            Scheduler.Advance(20.Seconds());
            probe.ExpectMsg<GetRoutees>();
            probe.ExpectNoMsg();
        }
    }
}