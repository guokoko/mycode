using System;
using System.Collections.Generic;
using System.Data;
using Akka.Routing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using RZ.Foundation;
using static RZ.Foundation.Prelude;
#pragma warning disable 8619

namespace CTO.Price.Shared.Domain
{
    public sealed class PriceModelKey
    {
        public static string PhysicalStoreChannel => null!;

        public PriceModelKey(string? channel, string store, string sku)
        {
            Channel = string.IsNullOrEmpty(channel) ? PhysicalStoreChannel : channel;
            Store = store;
            Sku = sku;
        }

        public string Channel { get; }
        public string Store { get; }
        public string Sku { get; }
        public override string ToString() => $"{Channel}.{Store}:{Sku}";

        public override bool Equals(object obj) => obj is PriceModelKey ak && ak.Channel == Channel && ak.Store == Store && ak.Sku == Sku;

        bool Equals(PriceModelKey other) {
            return Channel == other.Channel && Store == other.Store && Sku == other.Sku;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Store, Sku);
        }
    }
    
    [BsonIgnoreExtraElements]
    public sealed class PriceModel : IConsistentHashable
    {
        public PriceModelKey Key { get; set; } = null!;

        public decimal VatRate { get; set; }
        public PriceDescription? OriginalPrice { get; set; }
        public PriceDescription? SalePrice { get; set; }
        public PriceDescription? PromotionPrice { get; set; }

        public Dictionary<string, object>? AdditionalData { get; set; }
        public DateTime PriceTime { get; set; }
        
        public DateTime LastUpdate { get; set; }

        public static PriceModel EmptyPriceModel(PriceModelKey key, decimal vatRate) =>
            new PriceModel
            {
                Key = key,
                VatRate = vatRate,
                PriceTime = DateTime.MinValue,
                LastUpdate = DateTime.MinValue
            };

        [JsonIgnore] public Option<PriceDescription> SpecialPrice => Optional(PromotionPrice ?? SalePrice ?? OriginalPrice).Map(p => p.Vat == NormalPrice.Vat ? default : p);
        [JsonIgnore] public Option<PriceDescription> SpecialPriceJDASSP => Optional(PromotionPrice ?? OriginalPrice).Map(p => p.Vat == NormalPrice.Vat ? default : p);

        [JsonIgnore]
        public PriceDescription NormalPrice
        {
            get
            {
                if (AdditionalData != null && AdditionalData.TryGetValue("operation", out var operation) && operation?.ToString().Trim().ToLower() == "d")
                {
                    OriginalPrice = new PriceDescription();
                    return OriginalPrice;
                }

                return OriginalPrice ?? SalePrice ?? PromotionPrice ?? throw new ConstraintException($"No prices available for {Key}");
            }
        }
        [JsonIgnore]
        public bool NoPriceAvailable => (OriginalPrice ?? SalePrice ?? PromotionPrice) == null;
        
        [JsonIgnore]
        public object ConsistentHashKey => Key.Sku;
    }

    public sealed class PriceDescription
    {
        public decimal Vat { get; set; }
        public decimal NonVat { get; set; }

        [BsonRepresentation(BsonType.Document)]
        public DateTime? Start { get; set; }
        [BsonRepresentation(BsonType.Document)]
        public DateTime? End { get; set; }
        [BsonRepresentation(BsonType.Document)]
        public DateTime UpdateTime { get; set; }
    }
}