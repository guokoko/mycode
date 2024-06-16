using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Admin.Services;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AdminTests.Service
{
    public class AuditStorageTest
    {
        [Fact]
        public async Task AuditStorageTest_GetLogByEmail_ShouldReturnAuditLogAndCallFind()
        {
            // Arrange
            const string email = "unit@test.com";
            const AuditLogActionType actionType = AuditLogActionType.Login;
            const string logResult = "test audit logs";
            var id = new Guid("75cfca2d-d807-4a2c-98e6-cb15724ea465");
            var logTime = new DateTime(2020, 10, 20);

            var auditLog = new AuditLog
            {
                Id = id,
                Email = email,
                Action = actionType,
                Result = logResult,
                LogTime = logTime
            };
            var auditLogs = new[] {auditLog};
            var auditLogCursor = new Mock<IAsyncCursor<AuditLog>>();
            auditLogCursor.Setup(s => s.Current).Returns(auditLogs);
            auditLogCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            auditLogCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            var mongoCollection = new Mock<IMongoCollection<AuditLog>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<AuditLog>>(),
                    It.IsAny<FindOptions<AuditLog, AuditLog>>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(auditLogCursor.Object);
            var auditStorage = new AuditStorage(mongoCollection.Object);
            
            // Act
            var result = await auditStorage.GetLogByEmail(email);
            
            // Assert
            result.Count.Should().Be(1);
            result[0].Should().Be(auditLog);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<AuditLog>>(),
                It.IsAny<FindOptions<AuditLog, AuditLog>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task AuditStorageTest_NewDocument_ShouldCallInsertOne()
        {
            // Arrange
            const string email = "unit@test.com";
            const AuditLogActionType actionType = AuditLogActionType.Login;
            const string logResult = "test audit logs";
            var id = new Guid("75cfca2d-d807-4a2c-98e6-cb15724ea465");
            var logTime = new DateTime(2020, 10, 20);

            var auditLog = new AuditLog
            {
                Id = id,
                Email = email,
                Action = actionType,
                Result = logResult,
                LogTime = logTime
            };

            var mongoCollection = new Mock<IMongoCollection<AuditLog>>();
            mongoCollection.Setup(s => s.InsertOneAsync(auditLog, 
                null, It.IsAny<CancellationToken>()));
            var priceStorageTest = new AuditStorage(mongoCollection.Object);
            
            // Act
            await priceStorageTest.NewDocument(auditLog);
            
            // Assert
            mongoCollection.Verify(s => s.InsertOneAsync(It.IsAny<AuditLog>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AuditStorageTest_NewDocuments_ShouldCallInsertMany()
        {
            // Arrange
            const string email = "unit@test.com";
            const AuditLogActionType actionType = AuditLogActionType.Login;
            const string logResult = "test audit logs";
            var id = new Guid("75cfca2d-d807-4a2c-98e6-cb15724ea465");
            var logTime = new DateTime(2020, 10, 20);

            var auditLogs = new[] { new AuditLog
            {
                Id = id,
                Email = email,
                Action = actionType,
                Result = logResult,
                LogTime = logTime
            }};

            var mongoCollection = new Mock<IMongoCollection<AuditLog>>();
            mongoCollection.Setup(s => s.InsertManyAsync(auditLogs, 
                null, It.IsAny<CancellationToken>()));
            var priceStorageTest = new AuditStorage(mongoCollection.Object);
            
            // Act
            await priceStorageTest.NewDocuments(auditLogs);
            
            // Assert
            mongoCollection.Verify(s => s.InsertManyAsync(It.IsAny<IEnumerable<AuditLog>>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AuditStorageTest_UpdateDocument_ShouldReturnEditDataAndCallFindOneAndReplace()
        {
            // Arrange
            const string email = "unit@test.com";
            const AuditLogActionType actionType = AuditLogActionType.Login;
            const string logResult = "test audit logs";
            var id = new Guid("75cfca2d-d807-4a2c-98e6-cb15724ea465");
            var logTime = new DateTime(2020, 10, 20);

            var auditLog = new AuditLog
            {
                Id = id,
                Email = email,
                Action = actionType,
                Result = logResult,
                LogTime = logTime
            };

            var mongoCollection = new Mock<IMongoCollection<AuditLog>>();
            mongoCollection.Setup(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<AuditLog>>(),
                auditLog, It.IsAny<FindOneAndReplaceOptions<AuditLog, AuditLog>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(auditLog);
            var priceStorageTest = new AuditStorage(mongoCollection.Object);
            
            // Act
            var result = await priceStorageTest.UpdateDocument(auditLog, s => s.Equals(auditLog));
            
            // Assert
            result.Should().Be(auditLog);
            mongoCollection.Verify(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<AuditLog>>(),
                It.IsAny<AuditLog>(), It.IsAny<FindOneAndReplaceOptions<AuditLog, AuditLog>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AuditStorageTest_DeleteDocument_ShouldReturnDeleteDataAndCallFindOneAndDelete()
        {
            // Arrange
            const string email = "unit@test.com";
            const AuditLogActionType actionType = AuditLogActionType.Login;
            const string logResult = "test audit logs";
            var id = new Guid("75cfca2d-d807-4a2c-98e6-cb15724ea465");
            var logTime = new DateTime(2020, 10, 20);

            var auditLog = new AuditLog
            {
                Id = id,
                Email = email,
                Action = actionType,
                Result = logResult,
                LogTime = logTime
            };

            var mongoCollection = new Mock<IMongoCollection<AuditLog>>();
            mongoCollection.Setup(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<AuditLog>>(), 
                It.IsAny<FindOneAndDeleteOptions<AuditLog, AuditLog>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(auditLog);
            var priceStorageTest = new AuditStorage(mongoCollection.Object);
            
            // Act
            var result = await priceStorageTest.DeleteDocument(email, s => s.Equals(auditLog));
            
            // Assert
            result.Should().Be(auditLog);
            mongoCollection.Verify(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<AuditLog>>(),
                It.IsAny<FindOneAndDeleteOptions<AuditLog, AuditLog>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AuditStorageTest_DeleteDocuments_ShouldReturnDeletedRowAndCallDeleteMany()
        {
            // Arrange
            const string key = "unit@test.com";
            const long deletedCount = 1;
            
            var mongoCollection = new Mock<IMongoCollection<AuditLog>>();
            mongoCollection.Setup(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<AuditLog>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(new DeleteResult.Acknowledged(deletedCount));
            var priceStorageTest = new AuditStorage(mongoCollection.Object);
            
            // Act
            var result = await priceStorageTest.DeleteDocuments(key, s => s.Email.ToString().Equals(key));
            
            // Assert
            result.Should().Be(deletedCount);
            mongoCollection.Verify(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<AuditLog>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task AuditStorageTest_InsertLog_ShouldCallInsertOne()
        {
            // Arrange
            const string email = "unit@test.com";
            const AuditLogActionType actionType = AuditLogActionType.Login;
            const string logResult = "test audit logs";
            var id = new Guid("75cfca2d-d807-4a2c-98e6-cb15724ea465");
            var logTime = new DateTime(2020, 10, 20);

            var auditLog = new AuditLog
            {
                Id = id,
                Email = email,
                Action = actionType,
                Result = logResult,
                LogTime = logTime
            };

            var mongoCollection = new Mock<IMongoCollection<AuditLog>>();
            mongoCollection.Setup(s => s.InsertOneAsync(auditLog, 
                null, It.IsAny<CancellationToken>()));
            var priceStorageTest = new AuditStorage(mongoCollection.Object);
            
            // Act
            await priceStorageTest.InsertLog(auditLog);
            
            // Assert
            mongoCollection.Verify(s => s.InsertOneAsync(It.IsAny<AuditLog>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AuditStorageTest_GetTotalRecordWithFilter_ShouldCallCountDocuments()
        {
            // Arrange
            const long rowCount = 100;
            const string email = "unit@test.com";
            const AuditLogActionType actionType = AuditLogActionType.Login;
            var startFilter = new DateTime(2020, 10, 01);
            var endFilter = new DateTime(2020, 10, 31);

            var mongoCollection = new Mock<IMongoCollection<AuditLog>>();
            mongoCollection.Setup(s => s.CountDocumentsAsync(It.IsAny<FilterDefinition<AuditLog>>(),
                null, It.IsAny<CancellationToken>())).ReturnsAsync(rowCount);
            var auditStorage = new AuditStorage(mongoCollection.Object);
            
            // Act
            var result = await auditStorage.GetTotalRecordWithFilter(startFilter, endFilter, actionType, email);
            
            // Assert
            result.Should().Be(rowCount);
            mongoCollection.Verify(s => s.CountDocumentsAsync(It.IsAny<FilterDefinition<AuditLog>>(),
                null, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}