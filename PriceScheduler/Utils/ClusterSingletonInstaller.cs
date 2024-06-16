using System.Diagnostics.CodeAnalysis;
using Akka.Actor;
using Akka.Cluster.Tools.Singleton;

namespace CTO.Price.Scheduler.Utils
{
    [ExcludeFromCodeCoverage]
    public sealed class ClusterSingletonInstaller
    {
        readonly ActorSystem system;
        readonly ClusterSingletonManagerSettings managerSettings;
        readonly ClusterSingletonProxySettings proxySettings;
        public ClusterSingletonInstaller(ActorSystem system) {
            this.system = system;
            managerSettings = ClusterSingletonManagerSettings.Create(system);
            proxySettings = ClusterSingletonProxySettings.Create(system);
        }

        public IActorRef Build(Props props, string actorName) {
            system.ActorOf(ClusterSingletonManager.Props(singletonProps: props, terminationMessage: PoisonPill.Instance, managerSettings), actorName);
            return system.ActorOf(ClusterSingletonProxy.Props($"/user/{actorName}", proxySettings), $"{actorName}-proxy");
        }
    }
}