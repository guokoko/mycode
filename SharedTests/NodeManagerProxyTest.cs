using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Akka.Routing;
using Akka.TestKit.Xunit2;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace SharedTests
{
    public class NodeManagerProxyTest : TestKit
    {
        [Fact]
        public async Task NodeManagerProxy_ReceivedQueryAndResetPerformanceWithSingleNode_ReturnTotalPerformance()
        {
            // Assemble
            var nodeManagerProbe = this.CreateTestProbe();
            var nodeBroadCasterProbe = this.CreateTestProbe();
            var performanceStatistic = new PerformanceStatistic("test", 0, 0, 0, 0, 0, 0, new Dictionary<CodeBlock, CodeBlockPerformance>()
            {
                {CodeBlock.GetPrices, new CodeBlockPerformance()} 
            }, new Dictionary<string, int>()
            {
                {"Seed", 3}
            });
            var expectedQueryResult = TotalPerformance.New(1, new[] {performanceStatistic}.ToImmutableArray());
            var nodeManagerProxy = new NodeManagerProxy(Sys, nodeBroadCasterProbe.Ref, Mock.Of<ISystemLogService>());
            

            await Within(TimeSpan.FromSeconds(10), async () =>
            {
                // Act
                var resultTask = nodeManagerProxy.GetPerformanceAndReset();
                var firstMessage = nodeBroadCasterProbe.ReceiveOne();
                nodeBroadCasterProbe.Reply(new Routees(new []{Routee.FromActorRef(nodeManagerProbe.Ref)}));
                var secondMessage = nodeBroadCasterProbe.ReceiveOne();
                nodeBroadCasterProbe.Reply(performanceStatistic);
                var (expectedNodes, performanceStatistics) = await resultTask;
                
                // Assert
                firstMessage.Should().Be(GetRoutees.Instance);
                secondMessage.Should().Be(CTO.Price.Shared.Actor.NodeManagerCommands.QueryAndResetPerformance.New(nodeBroadCasterProbe.LastSender));
                
                expectedNodes.Should().Be(expectedQueryResult.ExpectedNodes);
                performanceStatistics[0].Should().Be(expectedQueryResult.Stats[0]);
            });
        }
        
        [Fact]
        public async Task NodeManagerProxy_ReceivedQueryAndResetPerformanceWithMultipleNodesAndAllAreResponsive_ReturnTotalPerformance()
        {
            // Assemble
            var nodeManagerProbe1 = this.CreateTestProbe();
            var nodeManagerProbe2 = this.CreateTestProbe();
            var nodeBroadCasterProbe = this.CreateTestProbe();
            var performanceStatistic = new PerformanceStatistic("test", 0, 0, 0, 0, 0, 0, new Dictionary<CodeBlock, CodeBlockPerformance>()
            {
                {CodeBlock.GetPrices, new CodeBlockPerformance()} 
            }, new Dictionary<string, int>()
            {
                {"Seed", 3}
            });
            var expectedQueryResult = TotalPerformance.New(2, new[] {performanceStatistic, performanceStatistic}.ToImmutableArray());
            var nodeManagerProxy = new NodeManagerProxy(Sys, nodeBroadCasterProbe.Ref, Mock.Of<ISystemLogService>());
            

            await Within(TimeSpan.FromSeconds(10), async () =>
            {
                // Act
                var resultTask = nodeManagerProxy.GetPerformanceAndReset();
                var firstMessage = nodeBroadCasterProbe.ReceiveOne();
                nodeBroadCasterProbe.Reply(new Routees(new []{Routee.FromActorRef(nodeManagerProbe1.Ref), Routee.FromActorRef(nodeManagerProbe2.Ref), }));
                var secondMessage = nodeBroadCasterProbe.ReceiveOne();
                nodeBroadCasterProbe.Reply(performanceStatistic);
                nodeBroadCasterProbe.Reply(performanceStatistic);
                var (expectedNodes, performanceStatistics) = await resultTask;
                
                // Assert
                firstMessage.Should().Be(GetRoutees.Instance);
                secondMessage.Should().Be(CTO.Price.Shared.Actor.NodeManagerCommands.QueryAndResetPerformance.New(nodeBroadCasterProbe.LastSender));
                
                expectedNodes.Should().Be(expectedQueryResult.ExpectedNodes);
                performanceStatistics[0].Should().Be(expectedQueryResult.Stats[0]);
                performanceStatistics[1].Should().Be(expectedQueryResult.Stats[1]);
            });
        }
        
        [Fact]
        public async Task NodeManagerProxy_ReceivedQueryAndResetPerformanceWithMultipleNodesAndSomeAreResponsive_ReturnTotalPerformance()
        {
            // Assemble
            var nodeManagerProbe1 = this.CreateTestProbe();
            var nodeManagerProbe2 = this.CreateTestProbe();
            var nodeBroadCasterProbe = this.CreateTestProbe();
            var performanceStatistic = new PerformanceStatistic("test", 0, 0, 0, 0, 0, 0, new Dictionary<CodeBlock, CodeBlockPerformance>()
            {
                {CodeBlock.GetPrices, new CodeBlockPerformance()} 
            }, new Dictionary<string, int>()
            {
                {"Seed", 3}
            });
            var expectedQueryResult = TotalPerformance.New(2, new[] {performanceStatistic}.ToImmutableArray());
            var nodeManagerProxy = new NodeManagerProxy(Sys, nodeBroadCasterProbe.Ref, Mock.Of<ISystemLogService>());
            

            await Within(TimeSpan.FromSeconds(10), async () =>
            {
                // Act
                var resultTask = nodeManagerProxy.GetPerformanceAndReset();
                var firstMessage = nodeBroadCasterProbe.ReceiveOne();
                nodeBroadCasterProbe.Reply(new Routees(new []{Routee.FromActorRef(nodeManagerProbe1.Ref), Routee.FromActorRef(nodeManagerProbe2.Ref), }));
                var secondMessage = nodeBroadCasterProbe.ReceiveOne();
                nodeBroadCasterProbe.Reply(performanceStatistic);
                var (expectedNodes, performanceStatistics) = await resultTask;
                
                // Assert
                firstMessage.Should().Be(GetRoutees.Instance);
                secondMessage.Should().Be(CTO.Price.Shared.Actor.NodeManagerCommands.QueryAndResetPerformance.New(nodeBroadCasterProbe.LastSender));
                
                expectedNodes.Should().Be(expectedQueryResult.ExpectedNodes);
                performanceStatistics.Length.Should().Be(1);
                performanceStatistics[0].Should().Be(expectedQueryResult.Stats[0]);
                
            });
        }
        
        [Fact]
        public async Task NodeManagerProxy_ReceivedQueryPerformance_ReturnTotalPerformance()
        {
            // Assemble
            var nodeManagerProbe = this.CreateTestProbe();
            
            var nodeBroadCasterProbe = this.CreateTestProbe();
            var performanceStatistic = new PerformanceStatistic("test", 0, 0, 0, 0, 0, 0, new Dictionary<CodeBlock, CodeBlockPerformance>()
            {
                {CodeBlock.GetPrices, new CodeBlockPerformance()} 
            }, new Dictionary<string, int>()
            {
                {"Seed", 3}
            });
            var expectedQueryResult = TotalPerformance.New(1, new[] {performanceStatistic}.ToImmutableArray());
            var nodeManagerProxy = new NodeManagerProxy(Sys, nodeBroadCasterProbe.Ref, Mock.Of<ISystemLogService>());
            

            await Within(TimeSpan.FromSeconds(10), async () =>
            {
                // Act
                var resultTask = nodeManagerProxy.GetPerformance();
                var firstMessage = nodeBroadCasterProbe.ReceiveOne();
                nodeBroadCasterProbe.Reply(new Routees(new []{Routee.FromActorRef(nodeManagerProbe.Ref)}));
                var secondMessage = nodeBroadCasterProbe.ReceiveOne();
                nodeBroadCasterProbe.Reply(performanceStatistic);
                var (expectedNodes, performanceStatistics) = await resultTask;
                
                // Assert
                firstMessage.Should().Be(GetRoutees.Instance);
                secondMessage.Should().Be(CTO.Price.Shared.Actor.NodeManagerCommands.QueryPerformance.New(nodeBroadCasterProbe.LastSender));
                
                expectedNodes.Should().Be(expectedQueryResult.ExpectedNodes);
                performanceStatistics[0].Should().Be(expectedQueryResult.Stats[0]);
            });
        }
        
        [Fact]
        public void NodeManagerProxy_ReceivedResetCommand_NodeBroadcasterReceiveResetCommand()
        {
            // Assemble
            var nodeManagerProbe = this.CreateTestProbe();
            var nodeBroadCasterProbe = this.CreateTestProbe();
            var nodeManagerProxy = new NodeManagerProxy(Sys, nodeBroadCasterProbe.Ref, Mock.Of<ISystemLogService>());
            

            Within(TimeSpan.FromSeconds(10), () =>
            {
                // Act
                nodeManagerProxy.Reset();
                
                // Assert
                nodeBroadCasterProbe.ExpectMsg(CTO.Price.Shared.Actor.NodeManagerCommands.ResetCounters.New());
            });
        }
    }
}