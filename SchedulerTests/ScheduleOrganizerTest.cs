using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using CTO.Price.Scheduler.Actors;
using CTO.Price.Scheduler.Actors.PriceImport;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RZ.Foundation;
using RZ.Foundation.Extensions;
using Xunit;

namespace SchedulerTests
{
    public class ScheduleOrganizerTest : TestKit
    {
        public ScheduleOrganizerTest() : base(@"akka.scheduler.implementation = ""Akka.TestKit.TestScheduler, Akka.TestKit""") { }
        private TestScheduler Scheduler => (TestScheduler)Sys.Scheduler;
        
        [Fact]
        public async Task PendingStartAndPendingEndSchedulesExist_TheTimeToStartAndEndIsInThePast_ShouldSendMessageToHeraldGroup()
        {
            //Arrange
            var probe = CreateTestProbe();
            
            probe.SetAutoPilot(new DelegateAutoPilot((sender,message) =>
            {
                if (message is GetRoutees)
                    sender.Tell(new Routees(new[] {new Routee()}));
                else
                    sender.Tell(ActorStatus.Complete.Instance, ActorRefs.NoSender);
                return AutoPilot.KeepRunning;
            }));
            
            var executedDateTimeUtc = new DateTime(2020, 12, 8, 0, 0, 0, DateTimeKind.Utc);
            var schedule1 = new Schedule()
            {
                Key = new ScheduleKey(new DateTime(2020, 07, 28).ToUniversalTime(),
                    new DateTime(2022, 11, 02).ToUniversalTime(), "CDS-Website", "10138", "SKU63"),
                Status = ScheduleStatus.PendingStart,
                PriceUpdate = new SchedulePriceUpdate(),
                LastUpdate = new DateTime(2020, 08, 06)
            };
            var pendingStartScheduleItems = (new[] {schedule1}).ToAsyncEnumerable();
            
            var schedule2 = new Schedule()
            {
                Key = new ScheduleKey(new DateTime(2020, 07, 28).ToUniversalTime(),
                    new DateTime(2020, 08, 02).ToUniversalTime(), "CDS-Website", "10138", "SKU635"),
                Status = ScheduleStatus.PendingEnd,
                PriceUpdate = new SchedulePriceUpdate(),
                LastUpdate = new DateTime(2020, 08, 06)
            };
            var pendingEndScheduleItems = (new[] {schedule2}).ToAsyncEnumerable();
            
            var actorEngineStartup = new Mock<IActorEngineStartup>();
            actorEngineStartup.Setup(e => e.ExceptionHandleActor).Returns(Mock.Of<IActorRef>());
            var services = new ServiceCollection();
            var scheduleService = new Mock<IScheduleService>();
            scheduleService
                .Setup(p => p.GetPendingStartSchedules(It.IsAny<DateTime>()))
                .Returns(pendingStartScheduleItems);
            scheduleService
                .Setup(p => p.GetPendingEndSchedules(It.IsAny<DateTime>())).Returns(pendingEndScheduleItems);
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(s => s.UtcNow()).Returns(executedDateTimeUtc);
            
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(scheduleService.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(Mock.Of<IScheduleStorage>());
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton(Mock.Of<ISystemLogService>());
            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            //Act
            var schedulerOrganizer = Sys.ActorOf(Props.Create(() => new ScheduleOrganizer(probe)));

            // Assert
            await AwaitConditionAsync(() => schedulerOrganizer.Ask(ActorCommand.ReplyIfReady.Instance).Result == ActorStatus.Ready.Instance);
            Scheduler.Advance(20.Seconds());
            await AwaitConditionAsync(() => schedulerOrganizer.Ask(ActorCommand.ReplyIfReady.Instance).Result == ActorStatus.Ready.Instance);
            probe.ReceiveN(2);
        }
        
        [Fact]
        public async Task MessageSentToHeraldPool_DidntReceiveReplyFirstMessage_LogExceptionAndRetry()
        {
            //Arrange
            var probe = CreateTestProbe();
            
            probe.SetAutoPilot(new DelegateAutoPilot((sender,message) =>
            {
                if (message is GetRoutees)
                {
                    sender.Tell(new Routees(new[] {new Routee()}));
                    return AutoPilot.KeepRunning;
                }

                return new DelegateAutoPilot((sender, message) =>
                {
                    if (message is GetRoutees)
                        sender.Tell(new Routees(new[] {new Routee()}));
                    else
                        sender.Tell(ActorStatus.Complete.Instance, ActorRefs.NoSender);
                    return AutoPilot.KeepRunning;
                });
            }));

            var executedDateTimeUtc = new DateTime(2020, 12, 8, 0, 0, 0, DateTimeKind.Utc);
            var schedule1 = new Schedule()
            {
                Key = new ScheduleKey(new DateTime(2020, 07, 28).ToUniversalTime(),
                    new DateTime(2022, 11, 02).ToUniversalTime(), "CDS-Website", "10138", "SKU63"),
                Status = ScheduleStatus.PendingStart,
                PriceUpdate = new SchedulePriceUpdate(),
                LastUpdate = new DateTime(2020, 08, 06)
            };
            var pendingStartScheduleItems = (new[] {schedule1}).ToAsyncEnumerable();
            
            var schedule2 = new Schedule()
            {
                Key = new ScheduleKey(new DateTime(2020, 07, 28).ToUniversalTime(),
                    new DateTime(2020, 08, 02).ToUniversalTime(), "CDS-Website", "10138", "SKU635"),
                Status = ScheduleStatus.PendingEnd,
                PriceUpdate = new SchedulePriceUpdate(),
                LastUpdate = new DateTime(2020, 08, 06)
            };
            var pendingEndScheduleItems = (new[] {schedule2}).ToAsyncEnumerable();
            
            var actorEngineStartup = new Mock<IActorEngineStartup>();
            actorEngineStartup.Setup(e => e.ExceptionHandleActor).Returns(Mock.Of<IActorRef>());
            var services = new ServiceCollection();
            var scheduleService = new Mock<IScheduleService>();
            scheduleService
                .Setup(p => p.GetPendingStartSchedules(It.IsAny<DateTime>()))
                .Returns(pendingStartScheduleItems);
            scheduleService
                .Setup(p => p.GetPendingEndSchedules(It.IsAny<DateTime>())).Returns(pendingEndScheduleItems);
            scheduleService.Setup(s => s.UpdateSchedules(It.IsAny<IEnumerable<Schedule>>())).Callback<IEnumerable<Schedule>>(updateList =>
            {
                foreach (var schedule in updateList)
                {
                    schedule.Status++;
                }
            });
            
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(s => s.UtcNow()).Returns(executedDateTimeUtc);

            var systemLogService = new Mock<ISystemLogService>();
            
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(scheduleService.Object);
            services.AddSingleton(actorEngineStartup.Object);
            services.AddSingleton(Mock.Of<IScheduleStorage>());
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton(systemLogService.Object);
            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            //Act
            var schedulerOrganizer = Sys.ActorOf(Props.Create(() => new ScheduleOrganizer(probe)));

            // Assert
            await AwaitConditionAsync(() => schedulerOrganizer.Ask(ActorCommand.ReplyIfReady.Instance).Result == ActorStatus.Ready.Instance);
            Scheduler.Advance(20.Seconds());
            await AwaitConditionAsync(() => schedulerOrganizer.Ask(ActorCommand.ReplyIfReady.Instance).Result == ActorStatus.Ready.Instance);
            probe.ExpectMsg<GetRoutees>();
            probe.ExpectMsg<PriceModel>();
            probe.ExpectMsg<PriceModel>();
            Scheduler.Advance(20.Seconds());
            await AwaitConditionAsync(() => schedulerOrganizer.Ask(ActorCommand.ReplyIfReady.Instance).Result == ActorStatus.Ready.Instance);
            probe.ExpectMsg<GetRoutees>();
            probe.ExpectMsg<PriceModel>();
        }
        
        [Fact]
        public async Task ScheduleOrganizerStartsProperly_WhenFinishedInitializing_ShouldCallScheduleOnce()
        {
            //Arrange
            var executedDateTimeUtc = new DateTime(2020, 12, 8, 0, 0, 0, DateTimeKind.Utc);
            var expectedDebugMessage = $"Scheduled price schedule check poll @ {executedDateTimeUtc}";
            var services = new ServiceCollection();
            var systemLog = new Mock<ISystemLogService>();
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(s => s.UtcNow()).Returns(executedDateTimeUtc);
            
            services.AddSingleton(dateTimeProvider.Object);
            services.AddSingleton(Mock.Of<IScheduleService>());
            services.AddSingleton(Mock.Of<IActorEngineStartup>());
            services.AddSingleton(Mock.Of<IScheduleStorage>());
            services.AddSingleton(Mock.Of<IEventLogService>());
            services.AddSingleton(systemLog.Object);
            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            //Act
            var priceImporter = Sys.ActorOf(Props.Create(() => new ScheduleOrganizer(TestActor)));

            // Assert
            await AwaitConditionAsync(() => priceImporter.Ask(ActorCommand.ReplyIfReady.Instance).Result == ActorStatus.Ready.Instance);
            systemLog.Verify(s => s.Debug(expectedDebugMessage), Times.Once);
        }
       
    }
}