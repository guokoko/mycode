using System.Diagnostics.CodeAnalysis;

namespace CTO.Price.Admin.Services
{
    [ExcludeFromCodeCoverageAttribute]
    public sealed class ServiceUris
    {
        public string Api { get; set; } = string.Empty;
        public string Scheduler { get; set; } = string.Empty;
    }
}