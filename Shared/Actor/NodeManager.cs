using System;
using System.Linq;
using Akka.Actor;
using Akka.Cluster;
using CTO.Price.Shared.Actor.NodeManagerCommands;
using CTO.Price.Shared.Services;
using LanguageExt;
using PerformanceStatistic = CTO.Price.Shared.Services.PerformanceStatistic;

namespace CTO.Price.Shared.Actor
{
    namespace NodeManagerCommands
    {
        // ReSharper disable UnusedMemberInSuper.Global
        // ReSharper disable UnusedParameter.Global
        [Union]
        public interface NodeManagerCommand
        {
            NodeManagerCommand QueryPerformance(IActorRef inquirer);
            NodeManagerCommand ResetCounters();
            NodeManagerCommand QueryAndResetPerformance(IActorRef inquirer);
        }
    }

    public sealed class NodeManager : ReceiveActor
    {
        public NodeManager(IPerformanceCounter performanceCounter) {
            var hostName = Context.System.Settings.Config.GetString("akka.remote.dot-netty.tcp.hostname");
            var port = Context.System.Settings.Config.GetString("akka.remote.dot-netty.tcp.port");
            var nodeName = $"{hostName}:{port}";


            Receive<QueryPerformance>(command =>
            {
                try
                {
                    var result = PerformanceStatistic.New(
                        nodeName,
                        performanceCounter.InboundCounter,
                        performanceCounter.InboundProcessedCounter,
                        performanceCounter.ImporterHeraldCounter,
                        performanceCounter.ScheduleOrganizerHeraldCounter,
                        performanceCounter.OutboundCounter,
                        performanceCounter.IgnoredCounter,
                        performanceCounter.PerformanceDictionary,
                        Cluster.Get(Context.System).SelfRoles.ToDictionary(s => s, _ => 1));
                    command.Inquirer.Tell(result);
                }
                catch (Exception e)
                {
                    throw e;
                }
            });
            
            Receive<ResetCounters>(command => performanceCounter.Reset());
            
            Receive<QueryAndResetPerformance>(command => {
                var result = PerformanceStatistic.New(
                    nodeName,
                    performanceCounter.InboundCounter,
                    performanceCounter.InboundProcessedCounter,
                    performanceCounter.ImporterHeraldCounter,
                    performanceCounter.ScheduleOrganizerHeraldCounter,
                    performanceCounter.OutboundCounter,
                    performanceCounter.IgnoredCounter,
                    performanceCounter.PerformanceDictionary,
                    Cluster.Get(Context.System).SelfRoles.ToDictionary(s => s, _ => 1));
                command.Inquirer.Tell(result);
                performanceCounter.Reset();
            });
        }
        
    }
}