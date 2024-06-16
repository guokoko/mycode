using System;
using System.Threading.Tasks;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Microsoft.JSInterop;
using Moq;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace SharedTests
{
    public class TimeZoneServiceTest
    {
        private readonly TestBed<TimeZoneService> testBed;
        public TimeZoneServiceTest(ITestOutputHelper testOutputHelper)
        {
            testBed = new TestBed<TimeZoneService>(testOutputHelper);
        }
        
        [Fact]
        public async Task TimeZoneIsInBangkok_GetLocalOffset_Get7HoursAsOffset()
        {
            // Arrange
            const int bangkokOffsetMinutes = 420;
            var bangkokOffset = TimeSpan.FromMinutes(bangkokOffsetMinutes);
            var jsRuntimeMock = new Mock<IJSRuntime>();
            jsRuntimeMock.Setup(s => s.InvokeAsync<int>("blazorGetTimezoneOffset", new object[0])).ReturnsAsync(bangkokOffsetMinutes);
            testBed.RegisterSingleton<IJSRuntime>(jsRuntimeMock.Object);
            
            // Act
            var timezoneService = testBed.CreateSubject();
            var localOffset = await timezoneService.GetLocalOffset();
            
            // Assert
            jsRuntimeMock.Verify(s => s.InvokeAsync<int>("blazorGetTimezoneOffset", new object[0]), Times.Once);
            localOffset.Should().Be(bangkokOffset);
        }
        
        [Fact]
        public async Task TimeZoneIsInBangkok_GetLocalDateTimeOffset_Get7HoursAsOffset()
        {
            // Arrange
            const int bangkokOffsetInMinutes = 420;
            var bangkokOffset = TimeSpan.FromMinutes(bangkokOffsetInMinutes);
            var jsRuntimeMock = new Mock<IJSRuntime>();
            jsRuntimeMock.Setup(s => s.InvokeAsync<int>("blazorGetTimezoneOffset", new object[0])).ReturnsAsync(bangkokOffsetInMinutes);
            testBed.RegisterSingleton<IJSRuntime>(jsRuntimeMock.Object);

            var localDateTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, bangkokOffset);
            var localDateTimeInUtc = localDateTime.ToUniversalTime();
            
            // Act
            var timezoneService = testBed.CreateSubject();
            var localOffset = await timezoneService.GetLocalDateTime(localDateTimeInUtc);
            
            // Assert
            jsRuntimeMock.Verify(s => s.InvokeAsync<int>("blazorGetTimezoneOffset", new object[0]), Times.Once);
            localOffset.Should().Be(localDateTime);
        }
    }
}