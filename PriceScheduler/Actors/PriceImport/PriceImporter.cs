using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Akka.Streams;
using Akka.Streams.Dsl;
using Confluent.Kafka;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RZ.Foundation;
using RZ.Foundation.Extensions;
using static RZ.Foundation.Prelude;

namespace CTO.Price.Scheduler.Actors.PriceImport
{
    public sealed class PriceImporter : ReceiveActor
    { 
        readonly IActorRef heraldPool;
        static readonly TimeSpan PollPeriod = 3.Seconds();
        static readonly TimeSpan KafkaMessageWaitPeriod = 5.Seconds();
        static readonly TimeSpan HeraldAskWaitPeriod = 300.Seconds();
        static readonly int ElementsBeforeRamping = 2000;

        readonly IPerformanceCounter performanceCounter;
        readonly ITopicChannel importChannel;
        readonly ISystemLogService systemLogService;
        
        #region Messages

        sealed class CheckPriceSchedule
        {
            public static readonly CheckPriceSchedule Instance = new CheckPriceSchedule();
        }
        #endregion

        public PriceImporter(IActorRef heraldPool)
        {
            this.heraldPool = heraldPool;
            var locator = Context.System.GetExtension<ServiceLocator>();
            
            var busOption = locator.GetService<IOptionsMonitor<MessageBusOption>>().CurrentValue;
            var messageBus = locator.GetService<IMessageBus>();
            performanceCounter = locator.GetService<IPerformanceCounter>();
            systemLogService = locator.GetService<ISystemLogService>();
            importChannel = messageBus.ListenTopic(busOption.PriceImport);

            ReceiveAsync<CheckPriceSchedule>(_ => CheckPriceAvailability());
            
            Receive<ActorCommand.ReplyIfReady>(_ => Sender.Tell(ActorStatus.Ready.Instance));
        }

        protected override void PreStart() {
            base.PreStart();
            systemLogService.Debug($"{nameof(PriceImporter)} is started on {Context.Self.Path}");
            SchedulePoll(Context.System, Self);
        }
        
        protected override void PreRestart(Exception reason, object message)
        {
            importChannel.Dispose();
        }

        List<ConsumeResult<string, string>> failList = new List<ConsumeResult<string, string>>(); 

        async Task CheckPriceAvailability()
        {
            var parallelism =  (await heraldPool.Ask<Routees>(GetRoutees.Instance)).Members.Count()*2;
            

            systemLogService.Info($"Error count from previous run {failList.Count}");
        
            var successSink = Sink.Aggregate<ConsumeResult<string, string>, List<ConsumeResult<string, string>>>(new List<ConsumeResult<string, string>>(), (list, element) =>
            {
                performanceCounter.CountInboundProcessed();
                list.Add(element);
                return list;
            });
                
            var failSink = Sink.Aggregate<ConsumeResult<string, string>, List<ConsumeResult<string, string>>>(new List<ConsumeResult<string, string>>(), (list, element) =>
            {
                list.Add(element);
                return list;
            });
            
            var runnableGraph = RunnableGraph.FromGraph(GraphDsl.Create(successSink, failSink, Keep.Both, (builder, success, fail) =>
            {
                var source = builder.Add(new Concat<ConsumeResult<string, string>,ConsumeResult<string, string>>(2));
                builder.From(Source.From(failList).MapMaterializedValue<(Task<List<ConsumeResult<string, string>>>, Task<List<ConsumeResult<string, string>>>)>(_ => (null, null)!)).To(source);
                builder.From(Source.From(GetMessages()).Take(ElementsBeforeRamping-failList.Count).MapMaterializedValue<(Task<List<ConsumeResult<string, string>>>, Task<List<ConsumeResult<string, string>>>)>(_ => (null, null)!)).To(source);

                var deserializeStep = builder.Add(Flow.Create<ConsumeResult<string, string>>().Select(result => (result, DeserializeRawPrice(result.Message.Value))));
                var deserializePartition = builder.Add(new Partition<(ConsumeResult<string, string>, ApiResult<RawPrice>)>(2, result => result.Item2.IsSuccess ? 1 : 0));

                var deserializationFailFlow = builder.Add(Flow.Create<(ConsumeResult<string, string>, ApiResult<RawPrice>)>()
                    .Select(a => a.Item1));
                
                var deserializationSuccessFlow = builder.Add(Flow.Create<(ConsumeResult<string, string>, ApiResult<RawPrice>)>()
                    .Select(a =>(a.Item1, a.Item2.GetSuccess())));

                var sendToHeraldFlow = builder.Add(Flow.Create<(ConsumeResult<string, string>, RawPrice)>()
                    .SelectAsyncUnordered(parallelism, input =>
                        heraldPool.Ask<ActorStatus.Complete>(input.Item2, HeraldAskWaitPeriod).ToApiResult().Map(a => (input.Item1, a))
                    ));
                
                var askHeraldPartition = builder.Add(new Partition<(ConsumeResult<string, string>, ApiResult<ActorStatus.Complete>)>(2, result => result.Item2.IsSuccess ? 1 : 0));
                var askHeraldSuccessFlow = builder.Add(Flow.Create<(ConsumeResult<string, string>, ApiResult<ActorStatus.Complete>)>().Select(a => a.Item1));
                var askHeraldFailFlow = builder.Add(Flow.Create<(ConsumeResult<string, string>, ApiResult<ActorStatus.Complete>)>().Select(x => x.Item1));

                var storeOffSetMerge = builder.Add(new Merge<ConsumeResult<string, string>>(2));

                builder.From(source).Via(deserializeStep).To(deserializePartition);
                builder.From(deserializePartition.Out(1)).Via(deserializationSuccessFlow).Via(sendToHeraldFlow).To(askHeraldPartition);
                builder.From(deserializePartition.Out(0)).Via(deserializationFailFlow).Via(storeOffSetMerge).To(success);
                builder.From(askHeraldPartition.Out(1)).Via(askHeraldSuccessFlow).To(storeOffSetMerge); 
                builder.From(askHeraldPartition.Out(0)).Via(askHeraldFailFlow).To(fail);

                return ClosedShape.Instance;
            }));

            var (successListTask, failListTask) = runnableGraph.Run(Context.Materializer());
            var successList = await successListTask;
            failList = await failListTask;

            if (successList.Count > 0 || failList.Count > 0) {
                systemLogService.Info($"Finished processing...success count {successList.Count}, failed count {failList.Count}");
                string jsonString = JsonConvert.SerializeObject(failList, Formatting.Indented);
                systemLogService.Info($"failed list: {jsonString}");
            }

            if (successList.Count > 0 && failList.Count == 0) {
                systemLogService.Info("Kafka Commit");
                importChannel.Commit();
            }
             SchedulePoll(Context.System, Self);
        }
        
        IEnumerable<ConsumeResult<string, string>> GetMessages()
        {
            ConsumeResult<string, string>? result;
            while ((result = importChannel.Consume(KafkaMessageWaitPeriod)) != null)
            {
                performanceCounter.CountInbound();
                yield return result;
            }
        }

        void SchedulePoll(ActorSystem system, ICanTell target) {
            system.Scheduler.ScheduleTellOnce(PollPeriod, target, CheckPriceSchedule.Instance, ActorRefs.NoSender);
            systemLogService.Debug($"Scheduled Import poll @ {DateTime.UtcNow}");
        }

        static ApiResult<RawPrice> DeserializeRawPrice(string message) =>
            Try(() =>
            {
                var payload = JsonConvert.DeserializeObject<RawPrice>(message);
                if ((payload.Version != RawPrice.GetVersionTag(1) && payload.Version != RawPrice.GetVersionTag(2)) || payload.Event != "price.raw")
                    throw new ArgumentException("Incorrect version or event type!");
                return payload;
            }).Try();
    }
}