using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using CTO.Price.Scheduler.Actors;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RZ.Foundation.Extensions;
using Xunit;

namespace SchedulerTests
{
    public class MetricSingletonTest : TestKit
    {
        public MetricSingletonTest()
            : base(@"akka.scheduler.implementation = ""Akka.TestKit.TestScheduler, Akka.TestKit""") { }
        
        private TestScheduler Scheduler => (TestScheduler)Sys.Scheduler;
        
        [Fact]
        public void MetricSingleton_CoverageMetricCode_ShouldBecomeReady()
        {
            //Arrange
            var performanceStatistic = new PerformanceStatistic("test", 0, 0, 0, 0, 0, 0, new Dictionary<CodeBlock, CodeBlockPerformance>()
            {
                {CodeBlock.UpdatePrice, new CodeBlockPerformance()},
                {CodeBlock.UpdateSchedule, new CodeBlockPerformance()},
                {CodeBlock.GetBaseAndChannelPrice, new CodeBlockPerformance()},
                {CodeBlock.PublishToKafka, new CodeBlockPerformance()},
                {CodeBlock.GetPrices, new CodeBlockPerformance()},
                {CodeBlock.UpdatePriceGetPrice, new CodeBlockPerformance()},
                {CodeBlock.UpdatePriceHaveNewPrice, new CodeBlockPerformance()},
                {CodeBlock.UpdatePriceCombinePrice, new CodeBlockPerformance()},
                {CodeBlock.UpdatePriceUpdatePrice, new CodeBlockPerformance()}
            }, new Dictionary<string, int>()
            {
                {"Seed", 3}
            });
            
            var performanceStatistics = ImmutableArray.Create(new PerformanceStatistic[] { performanceStatistic });
            var mockTotalPerformance = new TotalPerformance(1, performanceStatistics);
            var services = new ServiceCollection();
            var systemLogService = new Mock<ISystemLogService>();
            var nodeManagerProxy = new Mock<INodeManagerProxy>();
            nodeManagerProxy
                .Setup(p => p.GetPerformanceAndReset())
                .ReturnsAsync(mockTotalPerformance);
            
            services.AddSingleton(nodeManagerProxy.Object);
            services.AddSingleton(systemLogService.Object);

            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));
            
            //Act
            var metric = Sys.ActorOf(Props.Create(() => new MetricSingleton()));
            
            // Assert
            AwaitCondition(() => metric.Ask(ActorCommand.ReplyIfReady.Instance).Result == ActorStatus.Ready.Instance);
            Scheduler.Advance(TimeSpan.FromSeconds(10));
            AwaitAssert(() => systemLogService.Verify(v => v.Debug("Sending Metrics To New Relic"), Times.AtLeastOnce), 10.Seconds());
        }
    }
}