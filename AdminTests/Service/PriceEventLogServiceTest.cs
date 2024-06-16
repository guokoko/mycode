using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.TestKit.Xunit2;
using CTO.Price.Admin.Data;
using CTO.Price.Admin.Services;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Log;
using FluentAssertions;
using Moq;
using Xunit;

namespace AdminTests.Service
{
    public class PriceEventLogServiceTest : TestKit
    {
        [Fact]
        public async Task AuditService_GetTotalRecordWithFilter_ReturnNumberOfAuditLog()
        {
            //Arrange
            var from = new DateTime(2020, 08, 26).ToUniversalTime();
            var end = new DateTime(2020, 08, 27).ToUniversalTime();
            var eventEnum = EventEnum.UpdatePrice;
            var levelEnum = LogLevelEnum.info;
            var channel = "CDS";
            var store = "10138";
            var sku = "CDS04967";
            var numberOfAuditLog = 4;

            var priceEventLogStorage = new Mock<IPriceEventLogStorage>();
            priceEventLogStorage.Setup(p => p.GetTotalRecordWithFilter(@from, end, eventEnum, levelEnum, channel, store, sku)).ReturnsAsync(numberOfAuditLog);
            
            var service = new PriceEventLogService(priceEventLogStorage.Object);

            //Act
            var countResult = await service.GetTotalRecordWithFilter(@from, end, eventEnum, levelEnum, channel, store, sku);

            //Assert
            countResult.Should().Be(numberOfAuditLog);
        }
        
        [Fact]
        public async Task AuditService_GetAuditLogWithFilter_ReturnListAuditLog()
        {
            //Arrange
            var from = new DateTime(2020, 08, 26).ToUniversalTime();
            var end = new DateTime(2020, 08, 27).ToUniversalTime();
            var eventEnum = EventEnum.UpdatePrice;
            var levelEnum = LogLevelEnum.info;
            var channel = "CDS";
            var store = "10138";
            var sku = "CDS04967";
            var pageIndex = 1;
            var pageSize = 20;
            List<EventLog> events = new List<EventLog>();

            var priceEventLogStorage = new Mock<IPriceEventLogStorage>();
            priceEventLogStorage.Setup(p => p.GetPriceEventLogWithFilter(@from, end, eventEnum, levelEnum, channel, store, sku, pageIndex, pageSize)).ReturnsAsync(events);
            
            var service = new PriceEventLogService(priceEventLogStorage.Object);

            //Act
            var result = await service.GetPriceEventLogWithFilter(@from, end, eventEnum, levelEnum, channel, store, sku, pageIndex, pageSize);

            //Assert
            result.Should().Equal(events);
        }
    }
}