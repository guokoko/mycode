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
    public class SystemLogStorageTest
    {
        [Fact]
        public async Task SystemLogStorageTest_NewDocument_ShouldCallInsertOne()
        {
            // Arrange
            const string message = "Test logs";
            const string environment = "test environment";
            var systemLog = new SystemLog
            {
                Level = LogLevelEnum.warning,
                Environment = environment,
                Message = message
            };
            
            var mongoCollection = new Mock<IMongoCollection<SystemLog>>();
            mongoCollection.Setup(s => s.InsertOneAsync(systemLog, 
                null, It.IsAny<CancellationToken>()));
            var systemLogStorage = new SystemLogStorage(mongoCollection.Object);
            
            // Act
            await systemLogStorage.NewDocument(systemLog);
            
            // Assert
            mongoCollection.Verify(s => s.InsertOneAsync(It.IsAny<SystemLog>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SystemLogStorageTest_NewDocuments_ShouldCallInsertMany()
        {
            // Arrange
            const string message = "Test logs";
            const string environment = "test environment";
            var systemLog = new []{ new SystemLog
            {
                Level = LogLevelEnum.warning,
                Environment = environment,
                Message = message
            }};
            
            var mongoCollection = new Mock<IMongoCollection<SystemLog>>();
            mongoCollection.Setup(s => s.InsertManyAsync
                (systemLog, null, It.IsAny<CancellationToken>()));
            var systemLogStorage = new SystemLogStorage(mongoCollection.Object);
            
            // Act
            await systemLogStorage.NewDocuments(systemLog);
            
            // Assert
            mongoCollection.Verify(s => s.InsertManyAsync(It.IsAny<IEnumerable<SystemLog>>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SystemLogStorageTest_UpdateDocument_ShouldReturnEditDataAndCallFindOneAndReplace()
        {
            // Arrange
            const string message = "Test logs";
            const string environment = "test environment";
            var systemLog = new SystemLog
            {
                Level = LogLevelEnum.warning,
                Environment = environment,
                Message = message
            };
            
            var mongoCollection = new Mock<IMongoCollection<SystemLog>>();
            mongoCollection.Setup(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<SystemLog>>(),
                systemLog, It.IsAny<FindOneAndReplaceOptions<SystemLog, SystemLog>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(systemLog);
            var systemLogStorage = new SystemLogStorage(mongoCollection.Object);
            
            // Act
            var result = await systemLogStorage.UpdateDocument(systemLog, s => s.Equals(systemLog));
            
            // Assert
            result.Should().Be(systemLog);
            mongoCollection.Verify(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<SystemLog>>(),
                It.IsAny<SystemLog>(), It.IsAny<FindOneAndReplaceOptions<SystemLog, SystemLog>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SystemLogStorageTest_DeleteDocument_ShouldReturnDeleteDataAndCallFindOneAndDelete()
        {
            // Arrange
            const string key = "CDS-Website:10138:CDS00001";
            const string message = "Test logs";
            const string environment = "test environment";
            var systemLog = new SystemLog
            {
                Level = LogLevelEnum.warning,
                Environment = environment,
                Message = message
            };
            
            var mongoCollection = new Mock<IMongoCollection<SystemLog>>();
            mongoCollection.Setup(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<SystemLog>>(), 
                It.IsAny<FindOneAndDeleteOptions<SystemLog, SystemLog>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(systemLog);
            var systemLogStorage = new SystemLogStorage(mongoCollection.Object);
            
            // Act
            var result = await systemLogStorage.DeleteDocument(key, s => s.Equals(systemLog));
            
            // Assert
            result.Should().Be(systemLog);
            mongoCollection.Verify(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<SystemLog>>(),
                It.IsAny<FindOneAndDeleteOptions<SystemLog, SystemLog>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task SystemLogStorageTest_DeleteDocuments_ShouldReturnDeletedRowAndCallDeleteMany()
        {
            // Arrange
            const string key = "CDS-Website:10138:CDS00001";
            const long deletedCount = 1;
            
            var mongoCollection = new Mock<IMongoCollection<SystemLog>>();
            mongoCollection.Setup(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<SystemLog>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(new DeleteResult.Acknowledged(deletedCount));
            var systemLogStorage = new SystemLogStorage(mongoCollection.Object);
            
            // Act
            var result = await systemLogStorage.DeleteDocuments(key, s => s.Message.Equals(key));
            
            // Assert
            result.Should().Be(deletedCount);
            mongoCollection.Verify(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<SystemLog>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}