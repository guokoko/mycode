using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using CTO.Price.Shared.Services;
using RZ.Foundation.Extensions;
using PerformanceStatistic = CTO.Price.Shared.Services.PerformanceStatistic;
using TotalPerformance = CTO.Price.Shared.Services.TotalPerformance;

namespace CTO.Price.Shared.Actor
{
    public interface INodeManagerProxy
    {
        Task Initialize();
        Task<TotalPerformance> GetPerformance();
        Task<TotalPerformance> GetPerformanceAndReset();
        void Reset();
    }
    
    [ExcludeFromCodeCoverage]
    public sealed class NodeManagerProxy : INodeManagerProxy
    {
        readonly IActorRef nodeBroadcaster;
        readonly ActorSystem actorSystem;
        readonly ISystemLogService systemLogService;

        public NodeManagerProxy(ActorEngine actorEngine, ISystemLogService systemLogService) {
            actorSystem = actorEngine.System!;
            actorSystem.ActorOf(actorSystem.DI().Props<NodeManager>(), "node-manager");
            nodeBroadcaster = actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "nodes");
            this.systemLogService = systemLogService;
        }
        
        public NodeManagerProxy(ActorSystem actorSystem, IActorRef nodeBroadcaster, ISystemLogService systemLogService) {
            this.actorSystem = actorSystem;
            this.nodeBroadcaster = nodeBroadcaster;
            this.systemLogService = systemLogService;
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }
        
        public async Task<TotalPerformance> GetPerformance() => await QueryNodes(inbox => NodeManagerCommands.QueryPerformance.New(inbox.Receiver));
        public async Task<TotalPerformance> GetPerformanceAndReset() => await QueryNodes(inbox => NodeManagerCommands.QueryAndResetPerformance.New(inbox.Receiver));

        private async Task<TotalPerformance> QueryNodes(Func<Inbox, object> command)
        {
            try
            {
                using var inbox = Inbox.Create(actorSystem);
                inbox.Send(nodeBroadcaster, GetRoutees.Instance);

                var routees = (Routees) await inbox.ReceiveAsync(1.Seconds());
                var memberCount = routees.Members.Count();

                inbox.Send(nodeBroadcaster, command(inbox));
                var result = new List<PerformanceStatistic>();
                var watcher = Stopwatch.StartNew();


                while (result.Count < memberCount && watcher.Elapsed < TimeSpan.FromSeconds(3))
                {
                    var performanceStatistic = await inbox.ReceiveAsync(1.Seconds());

                    switch (performanceStatistic)
                    {
                        case PerformanceStatistic statistic:
                            result.Add(statistic);
                            break;

                        default:
                            systemLogService.Debug("Stat not send exception : default");
                            continue;
                    }
                }

                return TotalPerformance.New(memberCount, result.ToImmutableArray());
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        
        public void Reset() => nodeBroadcaster.Tell(NodeManagerCommands.ResetCounters.New());
        
    }
}