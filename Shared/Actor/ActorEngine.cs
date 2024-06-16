using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RZ.Foundation;
using RZ.Foundation.Extensions;

namespace CTO.Price.Shared.Actor
{
    public sealed class ActorEngineOption
    {
        public string Name { get; set; } = string.Empty;
        public string ConfigFile { get; set; } = string.Empty;
        public string InternalBinding { get; set; } = string.Empty;
        public string ExternalBinding { get; set; } = string.Empty;
        public string Seeds { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
    }

    public sealed class ServiceLocator : IExtension
    {
        readonly IServiceProvider container;
        public ServiceLocator(IServiceProvider container) {
            this.container = container;
        }

        public T GetService<T>() where T: notnull => container.GetService<T>();
    }

    public sealed class ServiceLocatorProvider : ExtensionIdProvider<ServiceLocator>
    {
        readonly IServiceProvider container;
        public ServiceLocatorProvider(IServiceProvider container) {
            this.container = container;
        }
        public override ServiceLocator CreateExtension(ExtendedActorSystem system) => new ServiceLocator(container);
    }

    public interface IActorEngineStartup
    {
        void Init(ActorSystem system, IServiceProvider serviceProvider);
        public IActorRef ExceptionHandleActor { get; set; }
    }
    
    public static class ActorEngineExtension
    {
        [ExcludeFromCodeCoverage]
        public static void SetupActorEngine<T>(this IServiceCollection services, IConfiguration configuration) where T: class, IActorEngineStartup, new() {
            services.Configure<ActorEngineOption>(configuration.GetSection("ActorSystem"));
            services.AddSingleton<IActorEngineStartup, T>();
            services.AddSingleton<ActorEngine, ActorEngine>();
        }

        [ExcludeFromCodeCoverage]
        public static void StartActorEngine<T>(this IApplicationBuilder app, IHostApplicationLifetime lifetime) where T: IActorEngineStartup, new() {
            var actorEngine = app.ApplicationServices.GetService<ActorEngine>();
            lifetime.ApplicationStopping.Register(() => actorEngine.StopAsync(CancellationToken.None).Wait(10.Seconds()));
            actorEngine.StartAsync(CancellationToken.None);
        }
    }

    public class ActorEngine
    {
        readonly IOptionsMonitor<ActorEngineOption> option;
        readonly IServiceProvider serviceProvider;
        readonly IActorEngineStartup startup;

        public ActorEngine(IOptionsMonitor<ActorEngineOption> option, IServiceProvider serviceProvider, IActorEngineStartup startup) {
            this.option = option;
            this.serviceProvider = serviceProvider;
            this.startup = startup;
        }

        public ActorSystem? System { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken) {
            var opt = option.CurrentValue;
            
            System = BuildActorSystem(opt);
            Cluster.Get(System).RegisterOnMemberUp(() => startup.Init(System, serviceProvider));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => System.TryAsync(Shutdown);

        ActorSystem BuildActorSystem(ActorEngineOption opt) {
            
            var actorConfig = string.IsNullOrEmpty(opt.ConfigFile)
                                  ? Config.Empty
                                  : ConfigurationFactory.ParseString(File.ReadAllText(opt.ConfigFile));
            
            var envConfig = string.IsNullOrEmpty(opt.ConfigFile)
                ? Config.Empty
                : ConfigurationFactory.ParseString(File.ReadAllText(opt.ConfigFile));

            var internalBinding = opt.InternalBinding.Split(':');
            var externalBinding = opt.ExternalBinding.Split(':');


            var remoteConfig = ConfigurationFactory.ParseString(
                $@"
                akka.remote.dot-netty.tcp {{
                    hostname = {internalBinding[0]}
                    port = {internalBinding[1]}
                    public-hostname = {externalBinding[0]}
                    public-port = {externalBinding[1]}
                    enforce-ip-family = true
                }}               
            ");
            
            
            var clusterConfig = ConfigurationFactory.ParseString($@"
                akka.cluster.seed-nodes = [{opt.Seeds.Split(",").Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => $"\"{s.Trim()}\"").Join(',')}]
                akka.cluster.roles = [{opt.Roles.Split(",").Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => $"\"{s.Trim()}\"").Join(',')}]
            ");

            var system = ActorSystem.Create(opt.Name, actorConfig.WithFallback(remoteConfig).WithFallback(clusterConfig));
            system.RegisterExtension(new ServiceLocatorProvider(serviceProvider));
            return system;
        }

        static async Task<Unit> Shutdown(ActorSystem system) {
            await CoordinatedShutdown.Get(system).Run(CoordinatedShutdown.ClrExitReason.Instance);
            return Unit.Value;
        }
    }
}