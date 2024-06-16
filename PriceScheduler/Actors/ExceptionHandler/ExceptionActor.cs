using System;
using System.Threading;
using Akka.Actor;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Services;
using Serilog;

namespace CTO.Price.Scheduler.Actors.ExceptionHandler
{
    public class ExceptionActor : UntypedActor
    {
        readonly ISystemLogService systemLogService;
        public ExceptionActor()
        {
            var locator = Context.System.GetExtension<ServiceLocator>();
            systemLogService = locator.GetService<ISystemLogService>();
            systemLogService.Debug($"An exception actor created at {Context.Self.Path}");
        }

        protected override void OnReceive(object message)
        {
            if (message is Exception exception)
                systemLogService.Error(exception, exception.Message);
        }
    }
}