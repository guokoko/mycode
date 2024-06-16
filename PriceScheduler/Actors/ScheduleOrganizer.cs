using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Routing;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Util;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Extensions;
using CTO.Price.Shared.Services;
using RZ.Foundation;
using RZ.Foundation.Extensions;
using static RZ.Foundation.Prelude;

namespace CTO.Price.Scheduler.Actors
{
    public class ScheduleOrganizer : ReceiveActor
    {
        static readonly TimeSpan PollPeriod = 10.Seconds();
        static readonly TimeSpan MessageWaitPeriod = 15.Seconds();

        readonly IEventLogService eventLogger;
        readonly IActorRef heraldPool;
        readonly IScheduleService scheduleService;
        readonly IActorRef exceptionHandleActor;
        readonly ISystemLogService systemLogService;
        readonly IDateTimeProvider dateTimeProvider;
        
        #region Messages
        
        public sealed class CheckPriceSchedule
        {
            public static readonly CheckPriceSchedule Instance = new CheckPriceSchedule();
        }

        #endregion

        public ScheduleOrganizer(IActorRef heraldPool)
        {
            this.heraldPool = heraldPool;
            
            var locator = Context.System.GetExtension<ServiceLocator>();
            var actorEngineStartup = locator.GetService<IActorEngineStartup>();
            
            scheduleService = locator.GetService<IScheduleService>();
            eventLogger = locator.GetService<IEventLogService>();
            systemLogService = locator.GetService<ISystemLogService>();
            dateTimeProvider = locator.GetService<IDateTimeProvider>();
            exceptionHandleActor = actorEngineStartup.ExceptionHandleActor;
            
            Receive<ActorCommand.ReplyIfReady>(_ => Sender.Tell(ActorStatus.Ready.Instance));
            ReceiveAsync<CheckPriceSchedule>(_ => CheckPriceAvailability());
        }
        

        protected override void PreStart()
        {
            base.PreStart();
            systemLogService.Debug($"{nameof(ScheduleOrganizer)} is started on {Context.Self.Path}");
            SchedulePoll(Context.System, Self);
        }
        
        async Task CheckPriceAvailability()
        {
            systemLogService.Debug("Checking price for price updates...");
            
            var routeesCount =  (await heraldPool.Ask<Routees>(GetRoutees.Instance)).Members.Count();
            
            var cutOffTime = dateTimeProvider.UtcNow();
            var schedulesToProcess = new List<Schedule>();
            var pendingStartScheduleEnumerator = scheduleService.GetPendingStartSchedules(cutOffTime).GetAsyncEnumerator();
            var pendingEndScheduleEnumerator = scheduleService.GetPendingEndSchedules(cutOffTime).GetAsyncEnumerator();
            
            while (await pendingEndScheduleEnumerator.MoveNextAsync())
            {
                var schedule = pendingEndScheduleEnumerator.Current;
                schedule.Status = ScheduleStatus.Completed;
                schedulesToProcess.Add(schedule);
            }

            while (await pendingStartScheduleEnumerator.MoveNextAsync())
            {
                var schedule = pendingStartScheduleEnumerator.Current;
                schedule.Status = ScheduleStatus.PendingEnd;
                schedulesToProcess.Add(schedule);
            }
            
            var successSink = Sink.Aggregate<Schedule, List<Schedule>>(new List<Schedule>(), (list, element) =>
            {
                list.Add(element);
                return list;
            });
                
            var failSink = Sink.Ignore<Schedule>();
            
            var runnableGraph = RunnableGraph.FromGraph(GraphDsl.Create(successSink, failSink, Keep.Both, (builder, success, fail) =>
            {
                var source = Source.From(schedulesToProcess.ToArray()).MapMaterializedValue<(Task<List<Schedule>>, Task)>(_ => (null, null)!);

                var transformToPriceModel = builder.Add(Flow.Create<Schedule>().Select(result => (result, result.ToPriceModel())));

                var sendToHeraldFlow = builder.Add(Flow.Create<(Schedule, PriceModel)>()
                    .SelectAsyncUnordered(routeesCount, input =>
                        heraldPool.Ask<ActorStatus.Complete>(input.Item2, MessageWaitPeriod).ToApiResult().Map(a => (input.Item1, a))
                    ));
                var askHeraldPartition = builder.Add(new Partition<(Schedule, ApiResult<ActorStatus.Complete>)>(2, result => result.Item2.IsSuccess ? 1 : 0));
                var askHeraldSuccessFlow = builder.Add(Flow.Create<(Schedule, ApiResult<ActorStatus.Complete>)>().Select(a => a.Item1));
                var askHeraldFailFlow = builder.Add(Flow.Create<(Schedule, ApiResult<ActorStatus.Complete>)>().Select(x => x.Item1));
                
                builder.From(source).Via(transformToPriceModel).Via(sendToHeraldFlow).To(askHeraldPartition);
                builder.From(askHeraldPartition.Out(1)).Via(askHeraldSuccessFlow).To(success); 
                builder.From(askHeraldPartition.Out(0)).Via(askHeraldFailFlow).To(fail);

                return ClosedShape.Instance;
            }));

            var (successListTask, failListTask) = runnableGraph.Run(Context.Materializer());
            var successList = await successListTask;
            await failListTask;
            
            await scheduleService.UpdateSchedules(successList);
            systemLogService.Debug($"Processed {successList.Count} schedules.");
            SchedulePoll(Context.System, Self);
        }

        void SchedulePoll(ActorSystem system, ICanTell target)
        {
            system.Scheduler.ScheduleTellOnce(PollPeriod, target, CheckPriceSchedule.Instance, ActorRefs.NoSender);
            systemLogService.Debug($"Scheduled price schedule check poll @ {dateTimeProvider.UtcNow()}");
        }
        protected override void PreRestart(Exception reason, object message)
        {
            exceptionHandleActor.Tell(reason);
        }
    }
}