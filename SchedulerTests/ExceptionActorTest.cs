using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using CTO.Price.Scheduler.Actors;
using CTO.Price.Scheduler.Actors.ExceptionHandler;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RZ.Foundation.Extensions;
using Xunit;

namespace SchedulerTests
{
    public class ExceptionActorTest : TestKit
    {
        [Fact]
        public void ExceptionActor_ThrowException_ShouldTellMessage()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<ISystemLogService>());
            Sys.RegisterExtension(new ServiceLocatorProvider(services.BuildServiceProvider()));

            // Act
            var exceptionActor = Sys.ActorOf(Props.Create(() => new ExceptionActor()));
            exceptionActor.Tell(new Exception());
            
            // Assert
            Within(5.Seconds(), () =>
            {
                ExpectNoMsg(1.Seconds());
            });
        }
    }
}