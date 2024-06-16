using System;
using System.Diagnostics.CodeAnalysis;
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Tools.Singleton;
using Akka.DI.Extensions.DependencyInjection;
using Akka.Routing;
using CTO.Price.Scheduler.Actors;
using CTO.Price.Scheduler.Actors.ExceptionHandler;
using CTO.Price.Scheduler.Actors.PriceImport;
using CTO.Price.Shared.Actor;
using Microsoft.Extensions.DependencyInjection;
using Petabridge.Cmd.Host;

namespace CTO.Price.Scheduler.Services
{
    [ExcludeFromCodeCoverage]
    class ActorEngineStartup : IActorEngineStartup
    {
        public void Init(ActorSystem actorSystem, IServiceProvider serviceProvider)
        {
            
            actorSystem.UseServiceProvider(serviceProvider);
            
            ExceptionHandleActor = actorSystem.ActorOf(Props.Create(() => new ExceptionActor()), "exception-actor");
            
            var heraldRouter = actorSystem.ActorOf(Props.Create(() => new PriceHerald())
                .WithRouter(FromConfig.Instance.WithSupervisorStrategy(new OneForOneStrategy(_ => Directive.Restart))), "heralds");

            var nodeRoles = Cluster.Get(actorSystem).SelfRoles;
            
            if (nodeRoles.Contains("metric"))
            {
                actorSystem.ActorOf(ClusterSingletonManager.Props(
                        singletonProps: Props.Create(() => new MetricSingleton()),
                        terminationMessage: PoisonPill.Instance,
                        settings: ClusterSingletonManagerSettings.Create(actorSystem).WithRole("metric")),
                    name: "metric-singleton");
            }

            if (nodeRoles.Contains("scheduler"))
            {
                actorSystem.ActorOf(ClusterSingletonManager.Props(
                        singletonProps: Props.Create(() => new ScheduleOrganizer(heraldRouter)),
                        terminationMessage: PoisonPill.Instance,
                        settings: ClusterSingletonManagerSettings.Create(actorSystem).WithRole("scheduler")),
                    name: "schedule-organizer");
            }
            
            if (nodeRoles.Contains("importer"))
            {
                actorSystem.ActorOf(Props.Create(() => new PriceImporter(heraldRouter)), "price-importer");
            }

            serviceProvider.GetRequiredService<INodeManagerProxy>().Initialize();

        }

        public IActorRef ExceptionHandleActor { get; set; } = null!;
    }
}