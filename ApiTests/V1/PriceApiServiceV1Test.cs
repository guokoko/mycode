using System.Collections.Generic;
using System.Threading.Tasks;
using CTO.Price.Api.Services;
using CTO.Price.Protos;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Moq;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ApiTests
{
    public class PriceApiServiceV1Test
    {
        readonly TestBed<PriceApiServiceV1> testBed;
        public PriceApiServiceV1Test(ITestOutputHelper output)
        {
            testBed = new TestBed<PriceApiServiceV1>(output);
            testBed.RegisterSingleton<IPerformanceCounter>(new PerformanceCounter());
        }
        
        [Fact]
        public async Task PriceApiServiceV1_GetPriceMetrics_ReturnMetricsAsInitial()
        {
            //Arrange
            const long totalPriceCount = 1000;
            const long totalScheduleCount = 1500;
            const long totalPendingStartSchedulesCount = 200;
            const long totalPendingEndSchedulesCount = 300;
            
            testBed.Fake<IPriceService>().Setup(s => s.TotalPriceCount())
                .ReturnsAsync(totalPriceCount);
            
            testBed.Fake<IScheduleService>().Setup(s => s.TotalScheduleCount())
                .ReturnsAsync(totalScheduleCount);
            
            testBed.Fake<IScheduleService>().Setup(s => s.TotalPendingStartSchedulesCount())
                .ReturnsAsync(totalPendingStartSchedulesCount);
            
            testBed.Fake<IScheduleService>().Setup(s => s.TotalPendingEndSchedulesCount())
                .ReturnsAsync(totalPendingEndSchedulesCount);
        
            var priceApiServiceV1 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV1.GetPriceMetrics(It.IsAny<Empty>(), It.IsAny<ServerCallContext>());
            
            //Assert
            result.TotalPrices.Should().Be(totalPriceCount);
            result.TotalSchedules.Should().Be(totalScheduleCount);
            result.TotalPendingStartSchedules.Should().Be(totalPendingStartSchedulesCount);
            result.TotalPendingEndSchedules.Should().Be(totalPendingEndSchedulesCount);
        }
    }
}