using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTO.Price.Shared;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Grpc.Core;
using LanguageExt;
using Moq;
using Xunit;

namespace SharedTests
{
    public class PerformanceCounterTest
    {

        [Fact]
        public void CallIncrementInboundProcessedCounter_InboundProcessedCounterIsZero_InboundProcessedCounterIsIncremented()
        {
            // Arrange
            var performanceCounter = new PerformanceCounter();
            
            // Act
            performanceCounter.CountInboundProcessed();
            
            // Assert
            performanceCounter.InboundProcessedCounter.Should().Be(1);
        }
    }
}