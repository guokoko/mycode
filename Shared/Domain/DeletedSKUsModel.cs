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
    public sealed class DeletedSKUsModelKey
    {
        public static string PhysicalStoreChannel => null!;

        public DeletedSKUsModelKey(string? channel, string store, string sku, string time)
        {
            Channel = string.IsNullOrEmpty(channel) ? PhysicalStoreChannel : channel;
            Store = store;
            Sku = sku;
            Time = time;
        }

        public string Channel { get; }
        public string Store { get; }
        public string Sku { get; }
        public string Time { get; }
        public override string ToString() => $"{Channel}.{Store}:{Sku}";

        public override bool Equals(object obj) => obj is DeletedSKUsModelKey ak && ak.Channel == Channel && ak.Store == Store && ak.Sku == Sku;

        bool Equals(DeletedSKUsModelKey other)
        {
            return Channel == other.Channel && Store == other.Store && Sku == other.Sku;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Store, Sku);
        }
    }

    [BsonIgnoreExtraElements]
    public sealed class DeletedSKUsModel : IConsistentHashable
    {
        public DeletedSKUsModelKey Key { get; set; } = null!;

        public decimal VatRate { get; set; }
        public DeletedSKUsDescription? OriginalPrice { get; set; }
        public DeletedSKUsDescription? SalePrice { get; set; }
        public DeletedSKUsDescription? PromotionPrice { get; set; }

        public Dictionary<string, object>? AdditionalData { get; set; }
        public DateTime PriceTime { get; set; }

        public DateTime LastUpdate { get; set; }

        public static DeletedSKUsModel EmptyDeletedSKUsModel(DeletedSKUsModelKey key, decimal vatRate) =>
            new DeletedSKUsModel
            {
                Key = key,
                VatRate = vatRate,
                PriceTime = DateTime.MinValue,
                LastUpdate = DateTime.MinValue
            };

        [JsonIgnore]
        public bool NoPriceAvailable => (OriginalPrice == null || OriginalPrice.IsDefault()) &&
                                    (SalePrice == null || SalePrice.IsDefault()) &&
                                    (PromotionPrice == null || PromotionPrice.IsDefault());

        [JsonIgnore]
        public object ConsistentHashKey => Key.Sku;
    }

    public sealed class DeletedSKUsDescription
    {
        public decimal Vat { get; set; }
        public decimal NonVat { get; set; }

        [BsonRepresentation(BsonType.Document)]
        public DateTime? Start { get; set; }
        [BsonRepresentation(BsonType.Document)]
        public DateTime? End { get; set; }
        [BsonRepresentation(BsonType.Document)]
        public DateTime UpdateTime { get; set; }

        public DeletedSKUsDescription(PriceDescription priceDescription)
        {
            if (priceDescription != null)
            {

                Vat = priceDescription.Vat;
                NonVat = priceDescription.NonVat;
                Start = priceDescription.Start;
                End = priceDescription.End;
                UpdateTime = priceDescription.UpdateTime;
            }


        }
        public bool IsDefault()
        {
            return Vat == 0 &&
                   NonVat == 0 &&
                   (!Start.HasValue || Start.Value == DateTime.MinValue) &&
                   (!End.HasValue || End.Value == DateTime.MinValue) &&
                   UpdateTime == DateTime.MinValue;
        }
    }
}