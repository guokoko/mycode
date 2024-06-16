using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CTO.Price.Api.Services;
using CTO.Price.Proto.V1;
using CTO.Price.Protos;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Moq;
using TestUtility;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ApiTests
{
    public class GetSchedulerTest
    {
        readonly TestBed<PriceApiServiceV1> testBed;

        public GetSchedulerTest(ITestOutputHelper output)
        {
            testBed = new TestBed<PriceApiServiceV1>(output);
            testBed.RegisterSingleton<IPerformanceCounter>(new PerformanceCounter());
        }
        
        [Fact]
        public async Task RequestScheduler_SchedulerExists_ReturnScheduler()
        {
            //Arrange
            const string bu = "CDS";
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS-0001";
            var start = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2020, 10, 31, 0, 0, 0, DateTimeKind.Utc);
            var lastUpdate = new DateTime(2020, 9, 30, 0, 0, 0, DateTimeKind.Utc);

            const decimal baseOriginalPriceVat = 214;
            const decimal baseOriginalPriceNonVat = 200;
            
            var schedulePrice = new SchedulePriceUpdate
            {
                OriginalPrice = new CTO.Price.Shared.Domain.PriceDescription
                {
                    Vat = baseOriginalPriceVat,
                    NonVat = baseOriginalPriceNonVat
                }
            };

            var schedule = new Schedule
            {
                Key = new ScheduleKey(start, end, channel, store, sku),
                PriceUpdate = schedulePrice,
                Status = ScheduleStatus.Completed,
                LastUpdate = lastUpdate
            };
            async IAsyncEnumerable<Schedule> GetMockSchedules()
            {
                yield return schedule;
                await Task.CompletedTask;
            }

            var getSchedulesParam = new GetSchedulesParam()
            {
                Bu = bu,
                Channel = channel,
                Store = store,
                Sku = sku
            };

            testBed.Fake<IScheduleService>()
                .Setup(s => s.GetSchedules(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(GetMockSchedules());
            var priceApiServiceV1 = testBed.CreateSubject();
            
            //Act
            var stream = new ServerStreamMock<GetSchedulesReply>();
            await priceApiServiceV1.GetSchedules(getSchedulesParam, stream, It.IsAny<ServerCallContext>());

            //Assert
            var result = stream.Messages.FirstOrDefault();
            result?.Start.Should().Be(start.ToTimestamp());
            result?.End.Should().Be(end.ToTimestamp());
            result?.OriginalPriceSchedule.Vat.Should()
                .Be(baseOriginalPriceVat.ToString(CultureInfo.InvariantCulture));
            result?.OriginalPriceSchedule.NonVat.Should()
                .Be(baseOriginalPriceNonVat.ToString(CultureInfo.InvariantCulture));
        }
    }
}