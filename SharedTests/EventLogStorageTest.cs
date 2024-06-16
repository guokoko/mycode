using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Log;
using CTO.Price.Shared.Services;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace SharedTests
{
    public class EventLogStorageTest
    {
        [Fact]
        public async Task EventLogStorage_NewDocument_ShouldCallInsertOne()
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
                Level = LogLevelEnum.debug
            };

            var mongoCollection = new Mock<IMongoCollection<EventLog>>();
            mongoCollection.Setup(s => s.InsertOneAsync(eventLog, 
                null, It.IsAny<CancellationToken>()));
            var eventLogStorage = new EventLogStorage(mongoCollection.Object);
            
            // Act
            await eventLogStorage.NewDocument(eventLog);
            
            // Assert
            mongoCollection.Verify(s => s.InsertOneAsync(It.IsAny<EventLog>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EventLogStorage_NewDocuments_ShouldCallInsertMany()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";
            const EventEnum eventEnum = EventEnum.GetPrice;
            object data = new {Code = 200, Message = ""};
            const string environment = "test environment";
            
            var eventLogs = new []{ new EventLog
            {
                Identifier = new EventLogKey(channel, store, sku),
                Event = eventEnum,
                State = StateEnum.Success,
                Data = data,
                Environment = environment,
                Level = LogLevelEnum.debug
            }};
            
            var mongoCollection = new Mock<IMongoCollection<EventLog>>();
            mongoCollection.Setup(s => s.InsertManyAsync
                (eventLogs, null, It.IsAny<CancellationToken>()));
            var eventLogStorage = new EventLogStorage(mongoCollection.Object);
            
            // Act
            await eventLogStorage.NewDocuments(eventLogs);
            
            // Assert
            mongoCollection.Verify(s => s.InsertManyAsync(It.IsAny<IEnumerable<EventLog>>(), 
                null, It.IsAny<CancellationToken>()), Times.Once);
            
        }

        [Fact]
        public async Task EventLogStorage_UpdateDocument_ShouldReturnEditDataAndCallFindOneAndReplace()
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
                Level = LogLevelEnum.debug
            };

            var mongoCollection = new Mock<IMongoCollection<EventLog>>();
            mongoCollection.Setup(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<EventLog>>(),
                eventLog, It.IsAny<FindOneAndReplaceOptions<EventLog, EventLog>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(eventLog);
            var eventLogStorage = new EventLogStorage(mongoCollection.Object);
            
            // Act
            var result = await eventLogStorage.UpdateDocument(eventLog, s => s.Equals(eventLog));
            
            // Assert
            result.Should().Be(eventLog);
            mongoCollection.Verify(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<EventLog>>(),
                eventLog, It.IsAny<FindOneAndReplaceOptions<EventLog, EventLog>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EventLogStorage_DeleteDocument_ShouldReturnDeleteDataAndCallFindOneAndDelete()
        {
            // Arrange
            const string key = "CDS-Website:10138:CDS00001";
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
                Level = LogLevelEnum.debug
            };

            var mongoCollection = new Mock<IMongoCollection<EventLog>>();
            mongoCollection.Setup(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<EventLog>>(), 
                It.IsAny<FindOneAndDeleteOptions<EventLog, EventLog>>(), It.IsAny<CancellationToken>())).ReturnsAsync(eventLog);
            var eventLogStorage = new EventLogStorage(mongoCollection.Object);
            
            // Act
            var result = await eventLogStorage.DeleteDocument(key, s => s.Equals(eventLog));
            
            // Assert
            result.Should().Be(eventLog);
            mongoCollection.Verify(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<EventLog>>(),
                It.IsAny<FindOneAndDeleteOptions<EventLog, EventLog>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EventLogStorage_DeleteDocuments_ShouldReturnDeletedRowAndCallDeleteMany()
        {
            // Arrange
            const string key = "CDS-Website:10138:CDS00001";
            const long deletedCount = 1;
            
            var mongoCollection = new Mock<IMongoCollection<EventLog>>();
            mongoCollection.Setup(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<EventLog>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(new DeleteResult.Acknowledged(deletedCount));
            var eventLogStorage = new EventLogStorage(mongoCollection.Object);
            
            // Act
            var result = await eventLogStorage.DeleteDocuments(key, s => s.Data.Equals(key));
            
            // Assert
            result.Should().Be(deletedCount);
            mongoCollection.Verify(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<EventLog>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}