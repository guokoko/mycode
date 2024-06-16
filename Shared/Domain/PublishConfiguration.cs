using System;
using System.Collections.Generic;

namespace CTO.Price.Shared.Domain
{
    public class PublishConfiguration
    {
        public Dictionary<string, string> StoreChannelMap { get; set; } = null!;
    }
}