using System.Threading;
using System.Threading.Tasks;
using CTO.Price.Scheduler.Actors;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using RZ.Foundation.Extensions;

namespace CTO.Price.Scheduler.Controllers
{
    [ApiController]
    [Route("")]
    public class NodeController : ControllerBase
    {
        private ActorEngine actorEngine;
        private ISystemLogService systemLogService;
        public NodeController(ActorEngine actorEngine, ISystemLogService systemLogService)
        {
            this.actorEngine = actorEngine;
            this.systemLogService = systemLogService;
        }

        [HttpGet("stop")]
        public async Task<IActionResult> LeaveCluster()
        {
            systemLogService.Debug("Stop called");
            return await actorEngine.StopAsync(CancellationToken.None).Map(Ok);
        }
    }
}