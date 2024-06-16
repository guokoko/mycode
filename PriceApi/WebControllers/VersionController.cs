using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace CTO.Price.Api.WebControllers
{
    [ApiController]
    [ExcludeFromCodeCoverage]
    [Route("[controller]")]
    public class VersionController : ControllerBase
    {
        public string Get() => Program.Version;
    }
}