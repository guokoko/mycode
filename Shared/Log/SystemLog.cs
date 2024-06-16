using System;
using CTO.Price.Shared.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CTO.Price.Shared.Log
{
    public class SystemLog
    {
        [BsonId]
        public Guid Key { get; set; } = Guid.NewGuid();
        
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public LogLevelEnum Level { get; set; }
        public string Environment { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string MessageDetail { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}