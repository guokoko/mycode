using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CTO.Price.Protos;
using CTO.Price.Shared.Extensions;
using CTO.Price.Shared.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace CTO.Price.Api.Services
{
    [ExcludeFromCodeCoverage]
    public class PerformanceService : PerformanceInfo.PerformanceInfoBase
    {
        readonly IPerformanceCounter perfCounter;

        public PerformanceService(IPerformanceCounter perfCounter) {
            this.perfCounter = perfCounter;
        }

        public override async Task<PerformanceReply> GetCounter(Empty request, ServerCallContext context) {
            return await Task.FromResult(GetStat());
        }

        public override async Task<PerformanceReply> ResetCounter(Empty request, ServerCallContext context) {
            var result = GetStat();
            perfCounter.Reset();
            return await Task.FromResult(result);
        }

        PerformanceReply GetStat() {
            var time = DateTime.UtcNow - perfCounter.StartWatch;
            var blockPerformance = perfCounter.PerformanceDictionary
                .Where(pm => pm.Key == CodeBlock.GetPrices)
                .Select(pm => $"{pm.Key}, {pm.Value.GetPerformance()}").ToArray();
            
            var result = new PerformanceReply
            {
                Inbound = perfCounter.InboundCounter.ToString(),
                Outbound = perfCounter.OutboundCounter.ToString(),
                GetApiPerformance = { blockPerformance },
                Ips = (perfCounter.InboundCounter / time.TotalSeconds).ToString(CultureInfo.InvariantCulture),
                Ops = (perfCounter.OutboundCounter / time.TotalSeconds).ToString(CultureInfo.InvariantCulture),
                Since = perfCounter.StartWatch.ToIsoFormat()
            };
            return result;
        }
    }
}