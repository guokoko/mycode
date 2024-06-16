using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CTO.Price.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;

namespace CTO.Price.Admin.Services
{
    public interface IPriceScheduler : IDisposable
    {
        Task<string> GetVersion();
    }

    [ExcludeFromCodeCoverageAttribute]
    public sealed class PriceScheduler : IPriceScheduler
    {
        readonly GrpcChannel channel;

        public PriceScheduler(IOptionsMonitor<ServiceUris> serviceUris) {
            channel = GrpcChannel.ForAddress(serviceUris.CurrentValue.Scheduler);
        }

        public void Dispose() {
            channel.Dispose();
        }

        public async Task<string> GetVersion() {
            var client = new ServiceInfo.ServiceInfoClient(channel);
            var response = await client.VersionAsync(new Empty());
            return response.Version;
        }
    }
}