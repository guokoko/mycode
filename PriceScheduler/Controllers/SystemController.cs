using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace CTO.Price.Scheduler.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SystemController : ControllerBase
    {
        readonly IHostApplicationLifetime lifetime;
        public SystemController(IHostApplicationLifetime lifetime) {
            this.lifetime = lifetime;
        }

        [HttpDelete]
        public IActionResult Shutdown() {
            lifetime.StopApplication();
            return Accepted("Shutdown is in progress.");
        }
    }
}