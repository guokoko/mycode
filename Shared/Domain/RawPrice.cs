using System;
using System.Collections.Generic;
using Akka.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RZ.Foundation.Extensions;

namespace CTO.Price.Shared.Domain
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public sealed class RawPriceDescription
    {
        private DateTime? start;
        private DateTime? end;
        public decimal? PriceVat { get; set; }
        public decimal? PriceNonVat { get; set; }
        public DateTime? Start
        {
            get => start;
            set => start = value.Try(d => DateTime.SpecifyKind(d, DateTimeKind.Utc));
        }
        public DateTime? End
        {
            get => end;
            set => end = value.Try(d => DateTime.SpecifyKind(d, DateTimeKind.Utc));
        }
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public sealed class RawPrice : IConsistentHashable
    {
        private DateTime timestamp;

        [JsonRequired]
        public string Version { get; set; } = string.Empty;
        [JsonRequired]
        public string Event { get; set; } = string.Empty;
        public string? Channel { get; set; }
        [JsonRequired]
        public string Store { get; set; } = string.Empty;
        [JsonRequired]
        public string Sku { get; set; } = string.Empty;

        /// <summary>
        /// This should be called VatPercent..
        /// </summary>
        public decimal? VatRate { get; set; }
        public RawPriceDescription? OriginalPrice { get; set; }
        public RawPriceDescription? SalePrice { get; set; }
        public RawPriceDescription? PromotionPrice { get; set; }

        /// <summary>
        /// Payload creation time (assume price time)
        /// </summary>
        [JsonRequired]
        public DateTime Timestamp
        {
            get => timestamp;
            set => timestamp = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        public Dictionary<string, object>? AdditionalData { get; set; }

        public static string GetVersionTag(int version) => $"price.v{version}";
        public object ConsistentHashKey => $"{Channel}.{Store}:{Sku}";
        public long DeliveryId { get; set; }
    }
}