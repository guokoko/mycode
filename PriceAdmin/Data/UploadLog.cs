using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CTO.Price.Admin.Data
{
    public class UploadLog
    {
        [BsonId]
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        
        [BsonRepresentation(BsonType.String)]
        public UploadResult Result { get; set; }
        public string Detail { get; set; } = string.Empty;
        public DateTime LogTime { get; set; } = DateTime.UtcNow;
    }

    public enum UploadResult
    {
        Success,
        Failure,
        S3Failure
    }
}