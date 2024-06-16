using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace CTO.Price.Api.WebControllers
{
    [ApiController]
    [ExcludeFromCodeCoverage]
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