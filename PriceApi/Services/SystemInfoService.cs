using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CTO.Price.Protos;
using CTO.Price.Shared.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using StackExchange.Redis;

namespace CTO.Price.Api.Services
{
    [ExcludeFromCodeCoverage]
    public class SystemInfoService : ServiceInfo.ServiceInfoBase
    {
        public override Task<VersionReply> Version(Empty request, ServerCallContext context)
        { 
            return Task.FromResult(new VersionReply {Version = Program.Version});
        }
    }
}