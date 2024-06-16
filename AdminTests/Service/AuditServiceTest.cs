using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.TestKit.Xunit2;
using CTO.Price.Admin.Data;
using CTO.Price.Admin.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace AdminTests.Service
{
    public class AuditServiceTest : TestKit
    {
        [Fact]
        public async Task AuditService_GetTotalRecordWithFilter_ReturnNumberOfAuditLog()
        {
            //Arrange
            var from = new DateTime(2020, 08, 01).ToUniversalTime();
            var end = new DateTime(2020, 08, 25).ToUniversalTime();
            var actionType = AuditLogActionType.All;
            var email = string.Empty;
            var numberOfAuditLog = 100;

            var auditStorage = new Mock<IAuditStorage>();
            auditStorage.Setup(p => p.GetTotalRecordWithFilter(from, end, actionType, email)).ReturnsAsync(numberOfAuditLog);
            
            var service = new AuditService(auditStorage.Object);

            //Act
            var countResult = await service.GetTotalRecordWithFilter(from, end, actionType, email);

            //Assert
            countResult.Should().Be(numberOfAuditLog);
        }
        
        [Fact]
        public async Task AuditService_GetAuditLogWithFilter_ReturnListAuditLog()
        {
            //Arrange
            var from = new DateTime(2020, 08, 01).ToUniversalTime();
            var end = new DateTime(2020, 08, 25).ToUniversalTime();
            var actionType = AuditLogActionType.Login;
            var email = string.Empty;
            var pageIndex = 1;
            var pageSize = 20;
            List<AuditLog> logs = new List<AuditLog>();

            var auditStorage = new Mock<IAuditStorage>();
            auditStorage.Setup(p => p.GetAuditLogWithFilter(from, end, actionType, email, pageIndex, pageSize)).ReturnsAsync(logs);
            
            var service = new AuditService(auditStorage.Object);

            //Act
            var result = await service.GetAuditLogWithFilter(from, end, actionType, email, pageIndex, pageSize);

            //Assert
             result.Should().Equal(logs);
        }

        [Fact]
        public async Task AuditService_CreateLogMessage_ShouldCallNewDocument()
        {
            // Arrange
            const string email = "unit@test.com";
            const AuditLogActionType actionType = AuditLogActionType.Login;
            var logTime = new DateTime(2020, 10, 31);

            var auditStorage = new Mock<IAuditStorage>();
            var auditService = new AuditService(auditStorage.Object);
            
            // Act
            await auditService.CreateLogMessage(email, actionType, logTime);
            
            // Assert
            auditStorage.Verify(v => v.NewDocument(It.IsAny<AuditLog>()), Times.Once);
        }
    }
}