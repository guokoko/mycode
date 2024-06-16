using System;
using System.Linq;
using System.Threading.Tasks;
using CTO.Price.Admin.Services;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Log;
using FluentAssertions;
using Moq;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace AdminTests.Service
{
    public class SystemLogServiceTest
    {
        readonly TestBed<SystemLogService> testBed;

        public SystemLogServiceTest(ITestOutputHelper output)
        {
            testBed = new TestBed<SystemLogService>(output);
        }

        [Fact]
        public async Task SystemLogService_GetTotalRecordWithFilter_ShouldReturnRowCountAndCallSystemLogStorage()
        {
            // Arrange
            const long rowCount = 100;
            
            var systemLogStorage = new Mock<ISystemLogStorage>();
            systemLogStorage.Setup(s => s.GetTotalRecordWithFilter(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<LogLevelEnum>(), It.IsAny<string>())).ReturnsAsync(rowCount);
            testBed.RegisterSingleton(systemLogStorage.Object);
            var systemLogService = testBed.CreateSubject();

            // Act
            var result = await systemLogService.GetTotalRecordWithFilter(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<LogLevelEnum>(), It.IsAny<string>());
            
            // Assert
            result.Should().Be(rowCount);
            systemLogStorage.Verify(s => s.GetTotalRecordWithFilter(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<LogLevelEnum>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SystemLogService_GetSystemLogWithFilter_ShouldReturnSystemLogsAndCallSystemLogStorage()
        {
            // Arrange
            const string message = "Test logs";
            const string environment = "test environment";
            const LogLevelEnum logLevel = LogLevelEnum.warning;
            var systemLogs = new[] {new SystemLog
            {
                Level = logLevel,
                Environment = environment,
                Message = message
            }}.ToList();
            
            var systemLogStorage = new Mock<ISystemLogStorage>();
            systemLogStorage.Setup(s => s.GetSystemLogWithFilter(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<LogLevelEnum>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(systemLogs);
            testBed.RegisterSingleton(systemLogStorage.Object);
            var systemLogService = testBed.CreateSubject();
            
            // Act
            var result = await systemLogService.GetSystemLogWithFilter(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<LogLevelEnum>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>());
            
            // Assert
            result.Count.Should().Be(1);
            result[0].Level.Should().Be(logLevel);
            result[0].Environment.Should().Be(environment);
            result[0].Message.Should().Be(message);
            systemLogStorage.Verify(s => s.GetSystemLogWithFilter(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<LogLevelEnum>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }
    }
}