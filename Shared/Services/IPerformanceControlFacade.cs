using System.Collections.Generic;
using System.Collections.Immutable;
using LanguageExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CTO.Price.Shared.Services
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    [Record]
    public partial struct CounterPerformance
    {
        public int Counter { get; }
        public double ExecutionInMilliseconds { get; }
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    [Record]
    public partial class PerformanceStatistic
    {
        public string NodeName { get; }

        public int InboundCounter { get; }
        public int InboundProcessedCounter { get; }
        public int HeraldFromImporterCount { get; }
        public int HeraldFromSchedulerCount { get; }
        public int OutBoundCounter { get; }
        public int IgnoredCounter { get; }
        public Dictionary<CodeBlock, CodeBlockPerformance> PerformanceDictionary { get; }
        public Dictionary<string, int> NodeRoles { get; }
        
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    [Record]
    public partial class TotalPerformance
    {
        public int ExpectedNodes { get; }
        public ImmutableArray<PerformanceStatistic> Stats { get; }
    }
}
