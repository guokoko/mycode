using System;
using System.Collections.Generic;
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
    public class OverlappingScheduleTest
    {
        readonly TestBed<ScheduleService> testBed;
        public OverlappingScheduleTest(ITestOutputHelper output)
        {
            testBed = new TestBed<ScheduleService>(output);
        }

        [Fact]
        public async Task CreateSchedule_ExistingScheduleStartsBeforeIncomingScheduleAndEndsBetweenIncomingSchedule_ExistingScheduleWillBeDeletedAndReplacedWithTheNewSchedule()
        {
            //Assemble
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS0001";

            var incomingStartDate = new DateTime(2020, 10, 5);
            var incomingEndDate = new DateTime(2020, 10, 15);
            
            var existingStartDate = new DateTime(2020, 10, 3);
            var existingEndDate = new DateTime(2020, 10, 8);
            
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
            
            var existingSchedule = new Schedule
            {
                Key = new ScheduleKey(existingStartDate, existingEndDate, channel, store, sku),
                PriceUpdate = new SchedulePriceUpdate(),
                Status = ScheduleStatus.PendingStart
            };

            var storage = new ScheduleStorageMock(new List<Schedule>() {existingSchedule});

            testBed.RegisterSingleton<IScheduleStorage>(storage);
            
            //Action
            await testBed.CreateSubject().UpdateSchedule(incoming, new DateTime(2020,9,30));
            
            //Assert
            (await storage.GetSchedule(new ScheduleKey(incomingStartDate, incomingEndDate, channel, store, sku))).Should().NotBeNull();
            storage.schedules.Count.Should().Be(1);

        }

        [Fact]
        public async Task CreateSchedule_ExistingScheduleStartsBetweenIncomingScheduleEndsAfterIncomingSchedule_ExistingScheduleWillBeDeletedAndReplacedWithTheNewSchedule()
        {
            //Assemble
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS0001";

            var incomingStartDate = new DateTime(2020, 10, 5);
            var incomingEndDate = new DateTime(2020, 10, 15);

            var existingStartDate = new DateTime(2020, 10, 8);
            var existingEndDate = new DateTime(2020, 10, 20);

            var incoming = new PriceModel
            {
                Key = new PriceModelKey(channel, store, sku),
                VatRate = 7,
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    Start = incomingStartDate,
                    End = incomingEndDate,
                    UpdateTime = new DateTime(2020, 9, 30)
                },
                PriceTime = new DateTime(2020, 9, 30),
                LastUpdate = new DateTime(2020, 9, 30)
            };

            var existingSchedule = new Schedule
            {
                Key = new ScheduleKey(existingStartDate, existingEndDate, channel, store, sku),
                PriceUpdate = new SchedulePriceUpdate(),
                Status = ScheduleStatus.PendingStart
            };

            var storage = new ScheduleStorageMock(new List<Schedule>() {existingSchedule});

            testBed.RegisterSingleton<IScheduleStorage>(storage);

            //Action
            await testBed.CreateSubject().UpdateSchedule(incoming, new DateTime(2020, 9, 30));

            //Assert
            (await storage.GetSchedule(new ScheduleKey(incomingStartDate, incomingEndDate, channel, store, sku))).Should().NotBeNull();
            storage.schedules.Count.Should().Be(1);
        }
        
        [Fact]
        public async Task CreateSchedule_ExistingScheduleStartsAndEndsBetweenIncomingSchedule_ExistingScheduleWillBeDeletedAndReplacedWithTheNewSchedule()
        {
            //Assemble
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS0001";

            var incomingStartDate = new DateTime(2020, 10, 5);
            var incomingEndDate = new DateTime(2020, 10, 15);
            
            var existingStartDate = new DateTime(2020, 10, 8);
            var existingEndDate = new DateTime(2020, 10, 12);
            
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
            
            var existingSchedule = new Schedule
            {
                Key = new ScheduleKey(existingStartDate, existingEndDate, channel, store, sku),
                PriceUpdate = new SchedulePriceUpdate(),
                Status = ScheduleStatus.PendingStart
            };

            var storage = new ScheduleStorageMock(new List<Schedule>() {existingSchedule});

            testBed.RegisterSingleton<IScheduleStorage>(storage);
            
            //Action
            await testBed.CreateSubject().UpdateSchedule(incoming, new DateTime(2020,9,30));
            
            //Assert
            (await storage.GetSchedule(new ScheduleKey(incomingStartDate, incomingEndDate, channel, store, sku))).Should().NotBeNull();
            storage.schedules.Count.Should().Be(1);
        }
        
        [Fact]
        public async Task CreateSchedule_ExistingScheduleStartsBeforeAndEndsAfterIncomingSchedule_ExistingScheduleWillBeDeletedAndReplacedWithTheNewSchedule()
        {
            //Assemble
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS0001";

            var incomingStartDate = new DateTime(2020, 10, 5);
            var incomingEndDate = new DateTime(2020, 10, 15);
            
            var existingStartDate = new DateTime(2020, 10, 1);
            var existingEndDate = new DateTime(2020, 10, 20);
            
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
            
            var existingSchedule = new Schedule
            {
                Key = new ScheduleKey(existingStartDate, existingEndDate, channel, store, sku),
                PriceUpdate = new SchedulePriceUpdate(),
                Status = ScheduleStatus.PendingStart
            };

            var storage = new ScheduleStorageMock(new List<Schedule>() {existingSchedule});

            testBed.RegisterSingleton<IScheduleStorage>(storage);
            
            //Action
            await testBed.CreateSubject().UpdateSchedule(incoming, new DateTime(2020,9,30));
            
            //Assert
            (await storage.GetSchedule(new ScheduleKey(incomingStartDate, incomingEndDate, channel, store, sku))).Should().NotBeNull();
            storage.schedules.Count.Should().Be(1);
        }
    }
}