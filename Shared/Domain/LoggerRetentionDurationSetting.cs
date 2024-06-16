using System;

namespace CTO.Price.Shared.Domain
{
    public sealed class LoggerRetentionDurationSetting
    {
        public LoggerRetentionDurationSetting()
            => ExpireAfter = new ExpireAfter();
        public ExpireAfter ExpireAfter { get; set; }
    }

    public class ExpireAfter
    {
        public int Days { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
    }
}