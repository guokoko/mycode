using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.TestKit.Xunit2;
using CTO.Price.Shared;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Moq;
using Xunit;
#pragma warning disable 1998

namespace SharedTests
{
    public class ScheduleServiceTest : TestKit
    {
        [Fact]
        public async Task ScheduleService_DeleteSchedule_Deleted()
        {
            //Arrange
            var key = new ScheduleKey(DateTime.Now, DateTime.Now.Add(TimeSpan.FromDays(1)), "channel", "store", "sku");
            var scheduleStorage = new Mock<IScheduleStorage>();
            var logService = new Mock<IEventLogService>();
            scheduleStorage.Setup(storage => storage.DeleteDocument(key.ToString(), s => s.Key == key && s.Status == ScheduleStatus.PendingStart)).ReturnsAsync(new Schedule());
            var scheduleService = new ScheduleService(scheduleStorage.Object, logService.Object);

            //Act
            var result = await scheduleService.DeleteSchedule(key);
            
            //Assert
            result.Should().BeEquivalentTo(UpdateResult.Deleted);
        }
        
        [Fact]
        public async Task ScheduleService_GetSchedules_ReturnData()
        {
            //Arrange
            string channel = "channel";
            string store = "store";
            string sku = "sku";
            var scheduleStorage = new Mock<IScheduleStorage>();
            var logService = new Mock<IEventLogService>();
            var scheduleService = new ScheduleService(scheduleStorage.Object, logService.Object);
            var scheduleItems = new[] { new Schedule(), new Schedule() };
            IAsyncEnumerable<Schedule> schedules = scheduleItems.ToAsyncEnumerable();

            scheduleStorage.Setup(scheduleStorage => scheduleStorage.GetSchedules(channel, store, sku)).Returns(schedules);

            //Act
            var result = scheduleService.GetSchedules(channel, store, sku);
            
            //Assert
            result.Should().BeEquivalentTo(scheduleItems);
            result.CountAsync().Result.Equals(2);
        }
        
        [Fact]
        public async Task ScheduleService_GetPendingStartSchedules_ReturnSameKey()
        {
            //Arrange
            var cutOffTime = new DateTime(2020, 08, 15).ToUniversalTime();
            
            var schedule1 = new Schedule() 
            { 
                Key = new ScheduleKey(new DateTime(2020, 07, 28).ToUniversalTime(), null, "CDS-Website", "10138", "SKU169"),
                Status = ScheduleStatus.PendingStart,
                LastUpdate = new DateTime(2020, 08, 06)
            };
            
            var schedule2 = new Schedule() 
            { 
                Key = new ScheduleKey(new DateTime(2020, 07, 28).ToUniversalTime(), new DateTime(2020, 08, 15).ToUniversalTime().AddDays(-1), "CDS-Website", "10138", "SKU105"),
                Status = ScheduleStatus.PendingStart,
                LastUpdate = new DateTime(2020, 08, 06)
            };

            var scheduleStorage = new Mock<IScheduleStorage>();
            var logService = new Mock<IEventLogService>();
            var scheduleService = new ScheduleService(scheduleStorage.Object, logService.Object);
            var scheduleItems = new[] {schedule1, schedule2 };
            IAsyncEnumerable<Schedule> schedules = scheduleItems.ToAsyncEnumerable();

            scheduleStorage.Setup(scheduleStorage => scheduleStorage.GetPendingStartSchedules(cutOffTime)).Returns(schedules);

            //Act
            var result = scheduleService.GetPendingStartSchedules(cutOffTime);
            
            //Assert
            var schedulers = await result.ToListAsync();
            schedulers[0].Key.Should().Be(schedule1.Key);
            schedulers[1].Key.Should().Be(schedule2.Key);
        }
        
        [Fact]
        public async Task ScheduleService_GetPendingEndSchedules_ReturnSameObject()
        {
            //Arrange
            var cutOffTime = new DateTime(2020, 08, 15).ToUniversalTime();
            
            var schedule1 = new Schedule() 
            { 
                Key = new ScheduleKey(new DateTime(2020, 07, 28).ToUniversalTime(), new DateTime(2020, 08, 02).ToUniversalTime(), "CDS-Website", "10138", "SKU169"),
                Status = ScheduleStatus.PendingEnd,
                LastUpdate = new DateTime(2020, 08, 06)
            };
            
            var schedule2 = new Schedule() 
            { 
                Key = new ScheduleKey(new DateTime(2020, 07, 28).ToUniversalTime(), new DateTime(2020, 08, 02).ToUniversalTime(), "CDS-Website", "10138", "SKU105"),
                Status = ScheduleStatus.PendingEnd,
                LastUpdate = new DateTime(2020, 08, 06)
            };

            var scheduleStorage = new Mock<IScheduleStorage>();
            var logService = new Mock<IEventLogService>();
            var scheduleService = new ScheduleService(scheduleStorage.Object, logService.Object);
            var scheduleItems = new[] {schedule1, schedule2 };
            IAsyncEnumerable<Schedule> schedules = scheduleItems.ToAsyncEnumerable();

            scheduleStorage.Setup(scheduleStorage => scheduleStorage.GetPendingEndSchedules(cutOffTime)).Returns(schedules);

            //Act
            var result = scheduleService.GetPendingEndSchedules(cutOffTime);
            
            //Assert
            var schedulers = await result.ToListAsync();
            schedulers[0].Should().Be(schedule1);
            schedulers[1].Should().Be(schedule2);
        }
        
        [Fact]
        public async Task ScheduleService_TotalScheduleCount_ShouldSameNumberOfSchedulerItemInitial()
        {
            //Arrange
            var items = 100;
            var scheduleStorage = new Mock<IScheduleStorage>();
            var logService = new Mock<IEventLogService>();
            scheduleStorage.Setup(storage => storage.TotalScheduleCount()).ReturnsAsync(items);
            var scheduleService = new ScheduleService(scheduleStorage.Object, logService.Object);

            //Act
            var result = await scheduleService.TotalScheduleCount();
            
            //Assert
            result.Should().Be(items);
        }
        
        [Fact]
        public async Task ScheduleService_TotalPendingStartSchedulesCount_ShouldSamePendingStartSchedulerItem()
        {
            //Arrange
            var items = 100;
            var scheduleStorage = new Mock<IScheduleStorage>();
            var logService = new Mock<IEventLogService>();
            scheduleStorage.Setup(storage => storage.TotalPendingStartSchedulesCount()).ReturnsAsync(items);
            var scheduleService = new ScheduleService(scheduleStorage.Object, logService.Object);

            //Act
            var result = await scheduleService.TotalPendingStartSchedulesCount();
            
            //Assert
            result.Should().Be(items);
        }
        
        [Fact]
        public async Task ScheduleService_TotalPendingEndSchedulesCount_ShouldSamePendingEndSchedulerItem()
        {
            //Arrange
            var items = 100;
            var scheduleStorage = new Mock<IScheduleStorage>();
            var logService = new Mock<IEventLogService>();
            scheduleStorage.Setup(storage => storage.TotalPendingEndSchedulesCount()).ReturnsAsync(items);
            var scheduleService = new ScheduleService(scheduleStorage.Object, logService.Object);

            //Act
            var result = await scheduleService.TotalPendingEndSchedulesCount();
            
            //Assert
            result.Should().Be(items);
        }
    }
}