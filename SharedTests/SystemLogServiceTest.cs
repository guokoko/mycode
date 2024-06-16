using System;
using System.Threading.Tasks;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Log;
using CTO.Price.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;
using ILogger = Serilog.ILogger;

namespace SharedTests
{
    public class SystemLogServiceTest
    {
        readonly TestBed<SystemLogService> testBed;

        public SystemLogServiceTest(ITestOutputHelper output)
        {
            testBed = new TestBed<SystemLogService>(output);
        }
        
        [Fact]
        public async Task SystemLogService_Warning_ShouldCallILoggerAndSystemLogStorage()
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


            var logger = new Mock<ILogger>();
            logger.Setup(s => s.Warning(message));
            testBed.RegisterSingleton(logger.Object);
            var systemLogStorage = new Mock<ISystemLogStorage>();
            systemLogStorage.Setup(s => s.NewDocument(systemLog));
            testBed.RegisterSingleton(systemLogStorage.Object);

            var systemLogService = testBed.CreateSubject();
            
            // Act
            await systemLogService.Warning(message);
            
            // Assert
            systemLogStorage.Verify(s => s.NewDocument(It.IsAny<SystemLog>()), Times.Once);
        }

        [Fact]
        public async Task SystemLogService_Error_ShouldCallILoggerAndSystemLogStorage()
        {
            // Arrange
            const string message = "Test logs";
            const string environment = "test environment";
            var exception = new Exception(message);
            var systemLog = new SystemLog
            {
                Level = LogLevelEnum.error,
                Environment = environment,
                Message = exception.Message,
            };


            var logger = new Mock<ILogger>();
            logger.Setup(s => s.Error(exception, message));
            testBed.RegisterSingleton(logger.Object);
            var systemLogStorage = new Mock<ISystemLogStorage>();
            systemLogStorage.Setup(s => s.NewDocument(systemLog));
            testBed.RegisterSingleton(systemLogStorage.Object);

            var systemLogService = testBed.CreateSubject();
            
            // Act
            await systemLogService.Error(exception, message);
            
            // Assert
            systemLogStorage.Verify(s => s.NewDocument(It.IsAny<SystemLog>()), Times.Once);
        }
    }
}