using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Services;
using RZ.Foundation.Extensions;

namespace CTO.Price.Scheduler.Actors
{
    public sealed class MetricSingleton : ReceiveActor
    {
        private static readonly TimeSpan PollPeriod = 60.Seconds();
        private const int TaskExecutionTimeout = 5000;

        sealed class SendNewRelicData
        {
            public static readonly SendNewRelicData Instance = new SendNewRelicData();
        }

        private readonly INodeManagerProxy nodeManagerProxy;
        private readonly ISystemLogService systemLogService;

        public MetricSingleton()
        {
            var locator = Context.System.GetExtension<ServiceLocator>();
            this.nodeManagerProxy = locator.GetService<INodeManagerProxy>();
            systemLogService = locator.GetService<ISystemLogService>();

            Receive<ActorCommand.ReplyIfReady>(_ => Sender.Tell(ActorStatus.Ready.Instance));
            ReceiveAsync<SendNewRelicData>(_ => ExecuteTask(GatherDataFromAllNodesAndSendDataToNewRelic(), TaskExecutionTimeout));
        }

        protected override void PreStart()
        {
            base.PreStart();
            ScheduleSendMetrics(Context.System, Self);
        }
        
        private async Task ExecuteTask(Task task, int timeout)
        {
            try
            {
                using var timeoutCancellationTokenSource = new CancellationTokenSource();
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    await task; // Very important in order to propagate exceptions
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
            catch (Exception e)
            {
                await systemLogService.Error(e, e.Message);
            }
        }

        private async Task GatherDataFromAllNodesAndSendDataToNewRelic()
        {
            systemLogService.Debug("Sending Metrics To New Relic");
            var (nodeCount, statistics) = await nodeManagerProxy.GetPerformanceAndReset();

            var codeBlocks = (CodeBlock[]) Enum.GetValues(typeof(CodeBlock));
            var processPerformances = codeBlocks
                .Select(c => (block: c, blockCountName: $"{c.ToString()}.count", blockTimeName: $"{c.ToString()}.time"))
                .SelectMany(record => new[]
                {
                    (record.blockCountName, (object) statistics.Sum(s => s.PerformanceDictionary[record.block].Times)),
                    (record.blockTimeName, (object) statistics.Sum(s => s.PerformanceDictionary[record.block].TotalMicroSeconds))
                }).ToDictionary(r => r.Item1, r => r.Item2);

            NewRelic.Api.Agent.NewRelic.RecordCustomEvent("ProcessPerformanceEvent", processPerformances);

            var nodeRoles = new Dictionary<string, int>();

            var a = statistics.Select(s => s.NodeRoles).ToList();
            foreach (var dic in a)
            {
                foreach (var (key, value) in dic)
                {
                    nodeRoles.TryGetValue(key, out var cur);
                    nodeRoles[key] = cur + value;
                }
            }

            NewRelic.Api.Agent.NewRelic.RecordCustomEvent("NodeRolesEvent", nodeRoles.ToDictionary(pair => pair.Key, pair => (object) pair.Value));

            var processCounters = new Dictionary<string, object>()
            {
                {"NodeCount", nodeCount},
                {"InboundCount", statistics.Sum(s => s.InboundCounter)},
                {"InboundProcessedCount", statistics.Sum(s => s.InboundProcessedCounter)},
                {"HeraldFromImporterCount", statistics.Sum(s => s.HeraldFromImporterCount)},
                {"HeraldFromSchedulerCount", statistics.Sum(s => s.HeraldFromSchedulerCount)},
                {"OutboundCount", statistics.Sum(s => s.OutBoundCounter)},
                {"IgnoredCount", statistics.Sum(s => s.IgnoredCounter)}
            };
            NewRelic.Api.Agent.NewRelic.RecordCustomEvent("ProcessCounterEvent", processCounters);
        }

        private static void ScheduleSendMetrics(ActorSystem system, ICanTell target)
        {
            system.Scheduler.ScheduleTellRepeatedly(0.Seconds(), PollPeriod, target, SendNewRelicData.Instance, ActorRefs.NoSender);
        }
    }
}