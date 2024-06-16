using System;
using Akka.Actor;
using Akka.DI.Extensions.DependencyInjection;
using Akka.Routing;
using CTO.Price.Shared.Actor;
using Microsoft.Extensions.DependencyInjection;
using Petabridge.Cmd.Host;

namespace CTO.Price.Api.Services
{
    class ActorEngineStartup : IActorEngineStartup
    {
        public void Init(ActorSystem actorSystem, IServiceProvider serviceProvider)
        {
            actorSystem.UseServiceProvider(serviceProvider);
            var heraldGroup = actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "price-heralds");

            serviceProvider.GetRequiredService<INodeManagerProxy>().Initialize();

            PetabridgeCmd.Get(actorSystem).Start();
            HeraldGroup = heraldGroup;
        }

        public IActorRef ExceptionHandleActor { get; set; } = null!;
        public IActorRef HeraldGroup { get; set; } = null!;
    }
}