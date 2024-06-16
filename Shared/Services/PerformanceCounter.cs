using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CTO.Price.Shared.Services
{
    public interface IPerformanceCounter
    {
        int InboundCounter { get; }
        int InboundProcessedCounter { get; }
        int OutboundCounter { get; }
        int ImporterHeraldCounter { get; }
        int ScheduleOrganizerHeraldCounter { get; }
        int IgnoredCounter { get; }
        public Dictionary<CodeBlock, CodeBlockPerformance> PerformanceDictionary { get; }

        DateTime StartWatch { get; }

        void CountInbound();
        void CountOutbound();
        void CountInboundProcessed();
        void CountHeraldFromImporter();
        void CountHeraldFromScheduler();
        void CountIgnored();

        public T CollectPerformance<T>(CodeBlock key, Func<T> func);
        public void CollectPerformance(CodeBlock key, Action action);
        public Task<T> CollectPerformance<T>(CodeBlock key, Func<Task<T>> func);
        
        void Reset();
    }
    public sealed class PerformanceCounter : IPerformanceCounter
    {
        int inbound;
        int inboundProcessed;
        int outbound;
        int heraldFromScheduler;
        int heraldFromImporter;
        int ignored;
        
        Dictionary<CodeBlock, CodeBlockPerformance> performanceDictionary = 
            ((CodeBlock[])Enum.GetValues(typeof(CodeBlock)))
            .ToDictionary(m => m, _ => new CodeBlockPerformance());

        public int InboundCounter => inbound;
        public int InboundProcessedCounter => inboundProcessed;
        public int OutboundCounter => outbound;
        public int ImporterHeraldCounter => heraldFromImporter;
        public int ScheduleOrganizerHeraldCounter => heraldFromScheduler;
        public int IgnoredCounter => ignored;
        public Dictionary<CodeBlock, CodeBlockPerformance> PerformanceDictionary => performanceDictionary;
        public DateTime StartWatch { get; private set; } = DateTime.UtcNow;
        

        public void CountInbound() {
            Interlocked.Increment(ref inbound);
        }

        public void CountOutbound() {
            Interlocked.Increment(ref outbound);
        }

        public void CountInboundProcessed()
        {
            Interlocked.Increment(ref inboundProcessed);
        }

        public void CountHeraldFromImporter() {
            Interlocked.Increment(ref heraldFromImporter);
        }

        public void CountHeraldFromScheduler()
        {
            Interlocked.Increment(ref heraldFromScheduler);
        }

        public void CountIgnored()
        {
            Interlocked.Increment(ref ignored);
        }

        public T CollectPerformance<T>(CodeBlock key, Func<T> func)
        {
            var methodPerformance = performanceDictionary.ContainsKey(key) ? performanceDictionary[key] : new CodeBlockPerformance();
            performanceDictionary[key] = methodPerformance;
            return methodPerformance.CollectPerformance(func);
        }

        public void CollectPerformance(CodeBlock key, Action action)
        {
            var methodPerformance = performanceDictionary.ContainsKey(key) ? performanceDictionary[key] : new CodeBlockPerformance();
            performanceDictionary[key] = methodPerformance;
            methodPerformance.CollectPerformance(action);
        }

        public async Task<T> CollectPerformance<T>(CodeBlock key, Func<Task<T>> func)
        {
            var methodPerformance = performanceDictionary.ContainsKey(key) ? performanceDictionary[key] : new CodeBlockPerformance();
            performanceDictionary[key] = methodPerformance;
            return await methodPerformance.CollectPerformance(func);
        }

        public void Reset() {
            StartWatch = DateTime.UtcNow;
            Interlocked.Exchange(ref inbound, 0);
            Interlocked.Exchange(ref inboundProcessed, 0);
            Interlocked.Exchange(ref outbound, 0);
            Interlocked.Exchange(ref heraldFromImporter, 0);
            Interlocked.Exchange(ref heraldFromScheduler, 0);
            Interlocked.Exchange(ref ignored, 0);
            
            performanceDictionary = 
                ((CodeBlock[])Enum.GetValues(typeof(CodeBlock)))
                .ToDictionary(m => m, _ => new CodeBlockPerformance());
        }
    }
    
    public class CodeBlockPerformance
    {
        private int times;
        private long totalMicroSeconds;
        private const long MilliToMicroFactor = 1000;

        public int Times => times;
        public long TotalMicroSeconds => totalMicroSeconds;
        
        public void CollectPerformance(Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action.Invoke();
            Interlocked.Add(ref totalMicroSeconds, (long) (stopwatch.Elapsed.TotalMilliseconds*MilliToMicroFactor));
            Interlocked.Increment(ref times);
        }

        public T CollectPerformance<T>(Func<T> func)
        {
            var stopwatch = Stopwatch.StartNew();
            var res = func.Invoke();
            Interlocked.Add(ref totalMicroSeconds, (long) (stopwatch.Elapsed.TotalMilliseconds*MilliToMicroFactor));
            Interlocked.Increment(ref times);
            return res;
        }

        public async Task<T> CollectPerformance<T>(Func<Task<T>> funTask)
        {
            var stopwatch = Stopwatch.StartNew();
            var res = await funTask.Invoke();
            Interlocked.Add(ref totalMicroSeconds, (long) (stopwatch.Elapsed.TotalMilliseconds*MilliToMicroFactor));
            Interlocked.Increment(ref times);
            return res;
        }

        public string GetPerformance() => times == 0
            ?$"Times called: {times}"
            :$"Times called: {times}, Total run time (Millisecond): {decimal.Divide(totalMicroSeconds, MilliToMicroFactor)}, Avg at (Millisecond/execution): {decimal.Divide(totalMicroSeconds, (times*MilliToMicroFactor))}";
    }

    public enum CodeBlock
    {
        UpdatePrice,
        UpdateSchedule,
        GetBaseAndChannelPrice,
        PublishToKafka,
        GetPrices,
        
        UpdatePriceGetPrice,
        UpdatePriceHaveNewPrice,
        UpdatePriceCombinePrice,
        UpdatePriceUpdatePrice,
    }
}