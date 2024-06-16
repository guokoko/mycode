using System;
using System.Threading;
using System.Threading.Tasks;
using CTO.Price.Admin.Services;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Log;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AdminTests.Service
{
    public class SystemLogStorageTest
    {
        [Fact]
        public async Task SystemLogStorage_GetTotalRecordWithFilter_ShouldReturnRowCountAndCallCountDocuments()
        {
            // Arrange
            const long rowCount = 100;
            var startFilter = new DateTime(2020, 10, 01);
            var endFilter = new DateTime(2020, 10, 31);

            var mongoCollection = new Mock<IMongoCollection<SystemLog>>();
            mongoCollection.Setup(s => s.CountDocumentsAsync(It.IsAny<FilterDefinition<SystemLog>>(),
                null, It.IsAny<CancellationToken>())).ReturnsAsync(rowCount);
            var systemLogStorage = new SystemLogStorage(mongoCollection.Object);
            
            // Act
            var result = await systemLogStorage.GetTotalRecordWithFilter(startFilter, endFilter,
                It.IsAny<string>(), It.IsAny<LogLevelEnum>(), It.IsAny<string>());
            
            // Assert
            result.Should().Be(rowCount);
            mongoCollection.Verify(s => s.CountDocumentsAsync(It.IsAny<FilterDefinition<SystemLog>>(),
                null, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}