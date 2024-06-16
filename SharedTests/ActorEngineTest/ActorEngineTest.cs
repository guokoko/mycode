using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using CTO.Price.Shared.Actor;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace SharedTests
{
    public class ActorEngineTest : TestKit
    {
        
        readonly TestBed<ActorEngine> testBed;

        public ActorEngineTest(ITestOutputHelper testOutputHelper)
        {
            testBed = new TestBed<ActorEngine>(testOutputHelper);
        }
        
        [Fact]
        public async Task ActorEngineSuppliedWithProperConfig_StartEngine_EngineConfigMatchesSuppliedConfig()
        {
            // Arrange
            const string engineName = "testEngine";
            const string nodeRole = "seed";
            const string host = "localhost";
            const int internalPort = 1234;
            const int externalPort = 1235;
            
            var actorEngineOption = new ActorEngineOption()
            {
                ConfigFile = "./ActorEngineTest/actor.conf",
                Name = engineName,
                Roles = nodeRole,
                Seeds = $"{host}:{externalPort}",
                InternalBinding = $"{host}:{internalPort}",
                ExternalBinding = $"{host}:{externalPort}"
            };

            testBed.RegisterSingleton(Mock.Of<IOptionsMonitor<ActorEngineOption>>(_ => _.CurrentValue == actorEngineOption));
            
            // Act
            var actorEngine = testBed.CreateSubject();
            await actorEngine.StartAsync(CancellationToken.None);

            // Assert
            var system = actorEngine.System;
            system.Should().NotBeNull();
            Debug.Assert(system != null, nameof(system) + " != null");
            system.Name.Should().Be(engineName);
            system.Settings.Config.GetValue("akka.cluster.seed-nodes").GetArray()[0].GetString().Should().Be($"{host}:{externalPort}");
            system.Settings.Config.GetValue("akka.cluster.roles").GetArray()[0].GetString().Should().Be(nodeRole);
            system.Settings.Config.GetValue("akka.remote.dot-netty.tcp.hostname").GetString().Should().Be(host);
            system.Settings.Config.GetValue("akka.remote.dot-netty.tcp.port").GetInt().Should().Be(internalPort);
            system.Settings.Config.GetValue("akka.remote.dot-netty.tcp.public-hostname").GetString().Should().Be(host);
            system.Settings.Config.GetValue("akka.remote.dot-netty.tcp.public-port").GetInt().Should().Be(externalPort);
        }
        
        [Fact]
        public async Task ActorEngineSuppliedWithProperConfig_StartEngineThenStop_EngineConfigMatchesSuppliedConfig()
        {
            // Arrange
            const string engineName = "testEngine";
            const string nodeRole = "seed";
            const string host = "localhost";
            const int internalPort = 1234;
            const int externalPort = 1235;
            
            var actorEngineOption = new ActorEngineOption()
            {
                ConfigFile = "./ActorEngineTest/actor.conf",
                Name = engineName,
                Roles = nodeRole,
                Seeds = $"{host}:{externalPort}",
                InternalBinding = $"{host}:{internalPort}",
                ExternalBinding = $"{host}:{externalPort}"
            };

            testBed.RegisterSingleton(Mock.Of<IOptionsMonitor<ActorEngineOption>>(_ => _.CurrentValue == actorEngineOption));

            var probe = CreateTestProbe();
            
            var actorEngine = testBed.CreateSubject();
            await actorEngine.StartAsync(CancellationToken.None);
            Debug.Assert(actorEngine.System != null, "actorEngine.System != null");
            probe.Watch(actorEngine.System.ActorSelection("/user").Anchor);
            
            // Act
            await actorEngine.StopAsync(CancellationToken.None);

            // Assert
            var msg = probe.ExpectMsg<Terminated>();
            Assert.Equal(msg.ActorRef, actorEngine.System.ActorSelection("/user").Anchor);
        }
    }
}