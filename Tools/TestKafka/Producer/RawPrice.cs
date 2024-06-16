using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RZ.Foundation.Extensions;

namespace Producer
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public sealed class RawPriceDescription
    {
        public decimal? PriceVat { get; set; }
        public decimal? PriceNonVat { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public sealed class RawPrice
    {
        [JsonRequired]
        public string Version { get; set; } = string.Empty;
        [JsonRequired]
        public string Event { get; set; } = string.Empty;
        [JsonRequired]
        public string Bu { get; set; } = string.Empty;
        [JsonRequired]
        public string Store { get; set; } = string.Empty;
        [JsonRequired]
        public string Sku { get; set; } = string.Empty;

        public decimal? VatRate { get; set; }
        public RawPriceDescription? OriginalPrice { get; set; }
        public RawPriceDescription? SalePrice { get; set; }
        public RawPriceDescription? PromotionPrice { get; set; }

        [JsonRequired]
        public DateTimeOffset Timestamp { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object>? ExtraData { get; set; }

        [JsonIgnore]
        public object? AdditionalData => ExtraData.Try(d => d["additional_data"]);

        public static string GetVersionTag(int version) => $"price.v{version}";
    }
}