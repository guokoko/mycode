using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using FluentAssertions;
using TestUtility;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace SchedulerTests.Scheduling
{
    public class NewScheduleTest
    {
        readonly TestBed<ScheduleService> testBed;
        public NewScheduleTest(ITestOutputHelper output)
        {
            testBed = new TestBed<ScheduleService>(output);
        }

        [Fact]
        public async Task CreateSchedule_IncomingScheduleIsInTheFuture_CreateScheduleWithStatusAsPendingStart()
        {
            //Assemble
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS0001";

            var incomingStartDate = new DateTime(2020, 10, 5);
            var incomingEndDate = new DateTime(2020, 10, 15);
            var now = new DateTime(2020, 9, 30);
            
            var incoming = new PriceModel
            {
                Key = new PriceModelKey(channel, store, sku),
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    Start = incomingStartDate,
                    End = incomingEndDate,
                    UpdateTime = new DateTime(2020,9,30)
                },
                PriceTime = new DateTime(2020,9,30),
                LastUpdate = new DateTime(2020,9,30)
            };

            var storage = new ScheduleStorageMock(new List<Schedule>());

            testBed.RegisterSingleton<IScheduleStorage>(storage);
            
            //Action
            await testBed.CreateSubject().UpdateSchedule(incoming, now);
            
            //Assert
            var schedule = await storage.GetSchedule(new ScheduleKey(incomingStartDate, incomingEndDate, channel, store, sku));
            schedule.Should().NotBeNull();
            Debug.Assert(schedule != null, nameof(schedule) + " != null");
            schedule.LastUpdate.Should().Be(now);
            schedule.Status.Should().Be(ScheduleStatus.PendingStart);
            storage.schedules.Count.Should().Be(1);
        }
        
        [Fact]
        public async Task CreateSchedule_IncomingScheduleInProgress_CreateScheduleWithStatusAsPendingEnd()
        {
            //Assemble
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS0001";

            var incomingStartDate = new DateTime(2020, 10, 5);
            var incomingEndDate = new DateTime(2020, 10, 15);
            var now = new DateTime(2020, 10, 10);
            
            var incoming = new PriceModel
            {
                Key = new PriceModelKey(channel, store, sku),
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    Start = incomingStartDate,
                    End = incomingEndDate,
                    UpdateTime = new DateTime(2020,9,30)
                },
                PriceTime = new DateTime(2020,9,30),
                LastUpdate = new DateTime(2020,9,30)
            };

            var storage = new ScheduleStorageMock(new List<Schedule>());

            testBed.RegisterSingleton<IScheduleStorage>(storage);
            
            //Action
            await testBed.CreateSubject().UpdateSchedule(incoming, now);
            
            //Assert
            var schedule = await storage.GetSchedule(new ScheduleKey(incomingStartDate, incomingEndDate, channel, store, sku));
            schedule.Should().NotBeNull();
            Debug.Assert(schedule != null, nameof(schedule) + " != null");
            schedule.LastUpdate.Should().Be(now);
            schedule.Status.Should().Be(ScheduleStatus.PendingEnd);
            storage.schedules.Count.Should().Be(1);
        }
    }
}