using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CTO.Price.Shared.Domain
{
    [BsonIgnoreExtraElements]
    public sealed class Schedule
    {
        [BsonId]
        public ScheduleKey Key { get; set; } = null!;
        public SchedulePriceUpdate PriceUpdate { get; set; } = null!;
        public ScheduleStatus Status { get; set; }
        public DateTime LastUpdate { get; set; }
        public override string ToString() => Key.ToString();
    }

    public sealed class ScheduleKey
    {
        [BsonRepresentation(BsonType.Document)]
        public DateTime? StartDate { get; set;}
        [BsonRepresentation(BsonType.Document)]
        public DateTime? EndDate { get; set; }
        public string Channel { get; set; }
        public string Store { get; set; }
        public string Sku { get; set; }

        public ScheduleKey(DateTime? startDate, DateTime? endDate, string channel, string store, string sku)
        {
            StartDate = startDate;
            EndDate = endDate;
            Channel = channel;
            Store = store;
            Sku = sku;
        }

        public override string ToString() =>
            $"{StartDate:u}.{EndDate:u}.{Channel}.{Store}.{Sku}";

        public override bool Equals(object? obj) => obj is ScheduleKey sk &&
                                                    sk.StartDate == StartDate &&
                                                    sk.EndDate == EndDate &&
                                                    sk.Channel == Channel &&
                                                    sk.Store == Store &&
                                                    sk.Sku == Sku;

        public override int GetHashCode()
        {
            return HashCode.Combine(StartDate, EndDate, Channel, Store, Sku);
        }
    }

    public sealed class SchedulePriceUpdate
    {
        public PriceDescription? OriginalPrice { get; set; }
        public PriceDescription? SalePrice { get; set; }
        public PriceDescription? PromotionPrice { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    public enum ScheduleStatus
    {
        PendingStart,
        PendingEnd,
        Completed
    }
}