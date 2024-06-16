using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace CTO.Price.Scheduler.Controllers
{
    [ExcludeFromCodeCoverage]
    [ApiController]
    [Route("[controller]")]
    public class VersionController : ControllerBase
    {
        public string Get() => Program.Version;
    }
}