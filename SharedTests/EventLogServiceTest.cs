using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Extensions;
using CTO.Price.Shared.Log;
using CTO.Price.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;
using TestUtility;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SharedTests
{
    [ExcludeFromCodeCoverage]
    public class EventLogServiceTest
    {
        readonly TestBed<EventLogService> testBed;

        public EventLogServiceTest(ITestOutputHelper output)
        {
            testBed = new TestBed<EventLogService>(output);
        }
        
        [Fact]
        public async Task EventLogService_Information_ILoggerAndEventLogStorageShouldBeCall()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";
            const EventEnum eventEnum = EventEnum.GetPrice;
            object data = new {Code = 200, Message = ""};
            const string environment = "test environment";
            var key = new PriceModelKey(channel, store, sku);
            
            var eventLog = new EventLog
            {
                Identifier = new EventLogKey(channel, store, sku),
                Event = eventEnum,
                State = StateEnum.Success,
                Data = data,
                Environment = environment,
                Level = LogLevelEnum.info
            };
            
            var eventLogStorage = new Mock<IEventLogStorage>();
            eventLogStorage.Setup(s => s.NewDocument(eventLog));
            testBed.RegisterSingleton(eventLogStorage.Object);
            var eventLogService = testBed.CreateSubject();

            // Act
            await eventLogService.Information(key, eventEnum, data);
            
            // Assert
            eventLogStorage.Verify(s => s.NewDocument(It.IsAny<EventLog>()), Times.Once);
        }
        
        [Fact]
        public async Task EventLogService_Warning_ILoggerAndEventLogStorageShouldBeCall()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";
            const EventEnum eventEnum = EventEnum.GetPrice;
            object data = new {Code = 200, Message = ""};
            const string environment = "test environment";
            
            var eventLog = new EventLog
            {
                Identifier = new EventLogKey(channel, store, sku),
                Event = eventEnum,
                State = StateEnum.Success,
                Data = data,
                Environment = environment,
                Level = LogLevelEnum.warning
            };
            
            var eventLogStorage = new Mock<IEventLogStorage>();
            eventLogStorage.Setup(s => s.NewDocument(eventLog));
            testBed.RegisterSingleton(eventLogStorage.Object);
            var eventLogService = testBed.CreateSubject();

            // Act
            await eventLogService.Warning(channel, store, sku, eventEnum, data);
            
            // Assert
            eventLogStorage.Verify(s => s.NewDocument(It.IsAny<EventLog>()), Times.Once);
        }
        
        [Fact]
        public async Task EventLogService_LogErrorUsingPriceModelKey_ILoggerAndEventLogStorageShouldBeCall()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";
            const EventEnum eventEnum = EventEnum.GetPrice;
            object data = new {Code = 200, Message = ""};
            const string environment = "test environment";
            
            var eventLog = new EventLog
            {
                Identifier = new EventLogKey(channel, store, sku),
                Event = eventEnum,
                State = StateEnum.Success,
                Data = data,
                Environment = environment,
                Level = LogLevelEnum.error
            };
            
            var eventLogStorage = new Mock<IEventLogStorage>();
            eventLogStorage.Setup(s => s.NewDocument(eventLog));
            testBed.RegisterSingleton(eventLogStorage.Object);
            var eventLogService = testBed.CreateSubject();

            // Act
            await eventLogService.Error(new PriceModelKey(channel, store, sku), eventEnum, data);
            
            // Assert
            eventLogStorage.Verify(s => s.NewDocument(It.IsAny<EventLog>()), Times.Once);
        }
        
        [Fact]
        public async Task EventLogService_Error_ILoggerAndEventLogStorageShouldBeCall()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";
            const EventEnum eventEnum = EventEnum.GetPrice;
            object data = new {Code = 200, Message = ""};
            const string environment = "test environment";
            
            var eventLog = new EventLog
            {
                Identifier = new EventLogKey(channel, store, sku),
                Event = eventEnum,
                State = StateEnum.Success,
                Data = data,
                Environment = environment,
                Level = LogLevelEnum.error
            };
            
            var eventLogStorage = new Mock<IEventLogStorage>();
            eventLogStorage.Setup(s => s.NewDocument(eventLog));
            testBed.RegisterSingleton(eventLogStorage.Object);
            var eventLogService = testBed.CreateSubject();

            // Act
            await eventLogService.Error(channel, store, sku, eventEnum, data);
            
            // Assert
            eventLogStorage.Verify(s => s.NewDocument(It.IsAny<EventLog>()), Times.Once);
        }
    }
}