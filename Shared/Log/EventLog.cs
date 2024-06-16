using System;
using CTO.Price.Shared.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CTO.Price.Shared.Log
{
    public class EventLog
    {
        [BsonId]
        public Guid Key { get; set; } = Guid.NewGuid();
        public EventLogKey Identifier { get; set; } = null!;
        
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public LogLevelEnum Level { get; set; }
        public string Environment { get; set; } = string.Empty;
        
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public EventEnum Event  { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public StateEnum State { get; set; }

        public object Data { get; set; } = new object();
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    public class EventLogKey
    {
        public EventLogKey(string? channel, string store, string sku) {
            Channel = channel;
            Store = store;
            Sku = sku;
        }
        public string? Channel { get; }
        public string Store { get; }
        public string Sku { get; }
        public override string ToString() => (string.IsNullOrEmpty(Channel) && string.IsNullOrEmpty(Store) && string.IsNullOrEmpty(Sku)) ? string.Empty : $"{Channel}.{Store}:{Sku}";
    }
}