using System;
using System.Collections.Generic;
using Akka.TestKit.Xunit2;
using CTO.Price.Scheduler.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace SchedulerTests.Controller
{
    public class SystemControllerTest : TestKit
    {
        [Fact]
        public void SystemController_Shutdown_ReturnSameMessage()
        {
            //Arrange
            var lifetime = new Mock<IHostApplicationLifetime>();
            lifetime.Setup(e => e.StopApplication());

            var controller = new SystemController(lifetime.Object);
            // Act
            var result = controller.Shutdown();

            // Assert
            var viewResult = Assert.IsType<AcceptedResult>(result);
            Assert.Equal(StatusCodes.Status202Accepted, viewResult.StatusCode);
        }
    }
}