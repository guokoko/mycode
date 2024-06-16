using System;
using System.Data;
using System.Threading.Tasks;
using CTO.Price.Api.Services;
using CTO.Price.Proto.V1;
using CTO.Price.Shared;
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
    public class DeleteScheduleTest
    {
        readonly TestBed<PriceApiServiceV1> testBed;

        public DeleteScheduleTest(ITestOutputHelper output)
        {
            testBed = new TestBed<PriceApiServiceV1>(output);
            testBed.RegisterSingleton<IPerformanceCounter>(new PerformanceCounter());
        }
        
        [Fact]
        public async Task DeleteSchedule_DeleteScheduleSuccess_ShouldReturnEmpty()
        {
            //Arrange
            const string bu = "CDS";
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
            var start = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2020, 10, 31, 0, 0, 0, DateTimeKind.Utc);
            
            var deleteScheduleParam = new DeleteScheduleParam
            {
                Bu = bu,
                Channel = channel,
                Store = store,
                Sku = sku,
                Start = start.ToTimestamp(),
                End = end.ToTimestamp()
            };

            testBed.Fake<IScheduleService>()
                .Setup(s => s.DeleteSchedule(It.IsAny<ScheduleKey>())).ReturnsAsync(UpdateResult.Deleted);
            
            var priceApiServiceV1 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV1.DeleteSchedule(deleteScheduleParam, It.IsAny<ServerCallContext>());
            
            //Assert
            result.GetType().Should().Be(typeof(Empty));
        }

        [Fact]
        public async Task DeleteSchedule_DeleteScheduleFailure_ShouldThrowRpcException()
        {
            //Arrange
            const string bu = "CDS";
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
            var start = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2020, 10, 31, 0, 0, 0, DateTimeKind.Utc);
            
            var deleteScheduleParam = new DeleteScheduleParam
            {
                Bu = bu,
                Channel = channel,
                Store = store,
                Sku = sku,
                Start = start.ToTimestamp(),
                End = end.ToTimestamp()
            };
            var key = new ScheduleKey(start, end, channel, store, sku);

            testBed.Fake<IScheduleService>()
                .Setup(s => s.DeleteSchedule(It.IsAny<ScheduleKey>()))
                .Throws(new ConstraintException(
                    $"Schedule {key} can not be deleted. Only inactive schedules can be deleted."));
            
            var priceApiServiceV1 = testBed.CreateSubject();
            
            var expectExpDetail = "Schedule 2020-10-01 00:00:00Z.2020-10-31 00:00:00Z.CDS-Website.10138.CDS-0001 can not be deleted. Only inactive schedules can be deleted.";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV1.DeleteSchedule(deleteScheduleParam, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.FailedPrecondition
                                                                       && e.Status.Detail == expectExpDetail);
        }
    }
}