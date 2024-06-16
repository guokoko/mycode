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
    public class PriceEventLogStorageTest
    {
        [Fact]
        public async Task PriceEventLogStorage_GetTotalRecordWithFilter_ShouldReturnRowCountAndCallCountDocuments()
        {
            // Arrange
            const long rowCount = 100;
            var startFilter = new DateTime(2020, 10, 01);
            var endFilter = new DateTime(2020, 10, 31);

            var mongoCollection = new Mock<IMongoCollection<EventLog>>();
            mongoCollection.Setup(s => s.CountDocumentsAsync(It.IsAny<FilterDefinition<EventLog>>(),
                null, It.IsAny<CancellationToken>())).ReturnsAsync(rowCount);
            var priceEventLogStorage = new PriceEventLogStorage(mongoCollection.Object);
            
            // Act
            var result = await priceEventLogStorage.GetTotalRecordWithFilter(startFilter, endFilter, 
                It.IsAny<EventEnum>(), It.IsAny<LogLevelEnum>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());
            
            // Assert
            result.Should().Be(rowCount);
            mongoCollection.Verify(s => s.CountDocumentsAsync(It.IsAny<FilterDefinition<EventLog>>(),
                null, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}