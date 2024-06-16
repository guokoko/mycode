using System;
using System.Linq;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Admin.Services;
using FluentAssertions;
using Moq;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace AdminTests.Service
{
    public class UploadLogServiceTest
    {
        readonly TestBed<UploadLogService> testBed;

        public UploadLogServiceTest(ITestOutputHelper output)
        {
            testBed = new TestBed<UploadLogService>(output);
        }

        [Fact]
        public async Task UploadLogService_CreateLogMessageWithoutDetail_ShouldCallUploadLogStorage()
        {
            // Arrange
            const string email = "unit@test.com";
            const string fileName = "example.csv";
            const UploadResult uploadResult = UploadResult.Success;

            var uploadLogStorage = new Mock<IUploadLogStorage>();
            testBed.RegisterSingleton(uploadLogStorage.Object);
            var uploadLogService = testBed.CreateSubject();

            // Act
            await uploadLogService.CreateLogMessage(email, fileName, uploadResult);
            
            // Assert
            uploadLogStorage.Verify(s => s.NewDocument(It.IsAny<UploadLog>()), Times.Once);
        }
        
        [Fact]
        public async Task UploadLogService_CreateLogMessageWithDetail_ShouldCallUploadLogStorage()
        {
            // Arrange
            const string email = "unit@test.com";
            const string fileName = "example.csv";
            const UploadResult uploadResult = UploadResult.Success;
            const string detail = "upload successful";

            var uploadLogStorage = new Mock<IUploadLogStorage>();
            testBed.RegisterSingleton(uploadLogStorage.Object);
            var uploadLogService = testBed.CreateSubject();

            // Act
            await uploadLogService.CreateLogMessage(email, fileName, uploadResult, detail);
            
            // Assert
            uploadLogStorage.Verify(s => s.NewDocument(It.IsAny<UploadLog>()), Times.Once);
        }

        [Fact]
        public async Task UploadLogService_GetTotalRecordWithFilter_ShouldReturnRowCountAndCallUploadLogStorage()
        {
            // Arrange
            const long rowCount = 100;
            
            var uploadLogStorage = new Mock<IUploadLogStorage>();
            uploadLogStorage.Setup(s => s.GetTotalRecordWithFilter(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<UploadResult>(), It.IsAny<string>())).ReturnsAsync(rowCount);
            testBed.RegisterSingleton(uploadLogStorage.Object);
            var uploadLogService = testBed.CreateSubject();
            
            // Act
            var result = await uploadLogService.GetTotalRecordWithFilter(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<UploadResult>(), It.IsAny<string>());

            result.Should().Be(rowCount);
            uploadLogStorage.Verify(s => s.GetTotalRecordWithFilter(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<UploadResult>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UploadLogService_GetUploadLogWithFilter_ShouldReturnSystemLogsAndCallUploadLogStorage()
        {
            // Arrange
            const string email = "unit@test.com";
            const string fileName = "example.csv";
            const UploadResult uploadResult = UploadResult.Success;
            const string detail = "upload successful";
            
            var guid = new Guid("b7e5bfe8-5032-4ace-9578-378340b5a70b");
            var uploadLogs = new[] { new UploadLog
            {
                Id = guid,
                Email = email,
                FileName = fileName,
                Result = uploadResult,
                Detail = detail
            }}.ToList();
            
            var uploadLogStorage = new Mock<IUploadLogStorage>();
            uploadLogStorage.Setup(s => s.GetUploadLogWithFilter(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<UploadResult>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(uploadLogs);
            testBed.RegisterSingleton(uploadLogStorage.Object);
            var uploadLogService = testBed.CreateSubject();

            var result = await uploadLogService.GetUploadLogWithFilter(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<UploadResult>(), It.IsAny<string>(), 
                It.IsAny<int>(), It.IsAny<int>());

            result.Count.Should().Be(1);
            result[0].Id.Should().Be(guid);
            result[0].Email.Should().Be(email);
            result[0].FileName.Should().Be(fileName);
            result[0].Result.Should().Be(uploadResult);
            result[0].Detail.Should().Be(detail);
            uploadLogStorage.Verify(s => s.GetUploadLogWithFilter(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<UploadResult>(), It.IsAny<string>(), 
                It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }
    }
}