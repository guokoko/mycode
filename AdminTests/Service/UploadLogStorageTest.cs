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
    public class UploadLogStorageTest
    {
        [Fact]
        public async Task UploadLogStorageTest_NewDocument_ShouldCallInsertOne()
        {
            // Arrange
            const string email = "unit@test.com";
            const string fileName = "uploadTest.csv";
            const UploadResult logResult = UploadResult.Success;
            const string detail = "test upload";
            var id = new Guid("75cfca2d-d807-4a2c-98e6-cb15724ea465");
            var logTime = new DateTime(2020, 10, 20);

            var uploadLog = new UploadLog
            {
                Id = id,
                Email = email,
                FileName = fileName,
                Result = logResult,
                Detail = detail,
                LogTime = logTime
            };

            var mongoCollection = new Mock<IMongoCollection<UploadLog>>();
            mongoCollection.Setup(s => s.InsertOneAsync(uploadLog, 
                null, It.IsAny<CancellationToken>()));
            var uploadLogStorage = new UploadLogStorage(mongoCollection.Object);
            
            // Act
            await uploadLogStorage.NewDocument(uploadLog);
            
            // Assert
            mongoCollection.Verify(s => s.InsertOneAsync(It.IsAny<UploadLog>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadLogStorageTest_NewDocuments_ShouldCallInsertMany()
        {
            // Arrange
            const string email = "unit@test.com";
            const string fileName = "uploadTest.csv";
            const UploadResult logResult = UploadResult.Success;
            const string detail = "test upload";
            var id = new Guid("75cfca2d-d807-4a2c-98e6-cb15724ea465");
            var logTime = new DateTime(2020, 10, 20);

            var uploadLogs = new[] { new UploadLog
            {
                Id = id,
                Email = email,
                FileName = fileName,
                Result = logResult,
                Detail = detail,
                LogTime = logTime
            }};

            var mongoCollection = new Mock<IMongoCollection<UploadLog>>();
            mongoCollection.Setup(s => s.InsertManyAsync(uploadLogs, 
                null, It.IsAny<CancellationToken>()));
            var uploadLogStorage = new UploadLogStorage(mongoCollection.Object);
            
            // Act
            await uploadLogStorage.NewDocuments(uploadLogs);
            
            // Assert
            mongoCollection.Verify(s => s.InsertManyAsync(It.IsAny<IEnumerable<UploadLog>>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadLogStorageTest_UpdateDocument_ShouldReturnEditDataAndCallFindOneAndReplace()
        {
            // Arrange
            const string email = "unit@test.com";
            const string fileName = "uploadTest.csv";
            const UploadResult logResult = UploadResult.Success;
            const string detail = "test upload";
            var id = new Guid("75cfca2d-d807-4a2c-98e6-cb15724ea465");
            var logTime = new DateTime(2020, 10, 20);

            var uploadLog = new UploadLog
            {
                Id = id,
                Email = email,
                FileName = fileName,
                Result = logResult,
                Detail = detail,
                LogTime = logTime
            };

            var mongoCollection = new Mock<IMongoCollection<UploadLog>>();
            mongoCollection.Setup(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<UploadLog>>(),
                uploadLog, It.IsAny<FindOneAndReplaceOptions<UploadLog, UploadLog>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(uploadLog);
            var uploadLogStorage = new UploadLogStorage(mongoCollection.Object);
            
            // Act
            var result = await uploadLogStorage.UpdateDocument(uploadLog, s => s.Equals(uploadLog));
            
            // Assert
            result.Should().Be(uploadLog);
            mongoCollection.Verify(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<UploadLog>>(),
                It.IsAny<UploadLog>(), It.IsAny<FindOneAndReplaceOptions<UploadLog, UploadLog>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadLogStorageTest_DeleteDocument_ShouldReturnDeleteDataAndCallFindOneAndDelete()
        {
            // Arrange
            const string email = "unit@test.com";
            const string fileName = "uploadTest.csv";
            const UploadResult logResult = UploadResult.Success;
            const string detail = "test upload";
            var id = new Guid("75cfca2d-d807-4a2c-98e6-cb15724ea465");
            var logTime = new DateTime(2020, 10, 20);

            var uploadLog = new UploadLog
            {
                Id = id,
                Email = email,
                FileName = fileName,
                Result = logResult,
                Detail = detail,
                LogTime = logTime
            };

            var mongoCollection = new Mock<IMongoCollection<UploadLog>>();
            mongoCollection.Setup(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<UploadLog>>(), 
                It.IsAny<FindOneAndDeleteOptions<UploadLog, UploadLog>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(uploadLog);
            var uploadLogStorage = new UploadLogStorage(mongoCollection.Object);
            
            // Act
            var result = await uploadLogStorage.DeleteDocument(email, s => s.Equals(uploadLog));
            
            // Assert
            result.Should().Be(uploadLog);
            mongoCollection.Verify(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<UploadLog>>(),
                It.IsAny<FindOneAndDeleteOptions<UploadLog, UploadLog>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadLogStorageTest_DeleteDocuments_ShouldReturnDeletedRowAndCallDeleteMany()
        {
            // Arrange
            const string key = "unit@test.com";
            const long deletedCount = 1;
            
            var mongoCollection = new Mock<IMongoCollection<UploadLog>>();
            mongoCollection.Setup(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<UploadLog>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(new DeleteResult.Acknowledged(deletedCount));
            var uploadLogStorage = new UploadLogStorage(mongoCollection.Object);
            
            // Act
            var result = await uploadLogStorage.DeleteDocuments(key, s => s.Email.ToString().Equals(key));
            
            // Assert
            result.Should().Be(deletedCount);
            mongoCollection.Verify(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<UploadLog>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task UploadLogStorageTest_GetTotalRecordWithFilter_ShouldReturnRowCountAndCallCountDocuments()
        {
            // Arrange
            const long rowCount = 100;
            var startFilter = new DateTime(2020, 10, 01);
            var endFilter = new DateTime(2020, 10, 31);

            var mongoCollection = new Mock<IMongoCollection<UploadLog>>();
            mongoCollection.Setup(s => s.CountDocumentsAsync(It.IsAny<FilterDefinition<UploadLog>>(),
                null, It.IsAny<CancellationToken>())).ReturnsAsync(rowCount);
            var uploadLogStorage = new UploadLogStorage(mongoCollection.Object);
            
            // Act
            var result = await uploadLogStorage.GetTotalRecordWithFilter(startFilter, endFilter, 
                It.IsAny<string>(), It.IsAny<UploadResult>(), It.IsAny<string>());
            
            // Assert
            result.Should().Be(rowCount);
            mongoCollection.Verify(s => s.CountDocumentsAsync(It.IsAny<FilterDefinition<UploadLog>>(),
                null, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}