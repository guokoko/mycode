using System;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Converters;

namespace CTO.Price.Admin.Data
{
    public class AuditLog
    {
        [BsonId]
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        
        [BsonRepresentation(BsonType.String)]
        public AuditLogActionType Action { get; set; }
        public string Result { get; set; } = null!;
        public string SystemUsingApi { get; set; } = null!;
        public DateTime LogTime { get; set; } = DateTime.UtcNow;
    }

    public enum AuditLogActionType
    {
        All,
        Login,
        Logout,
        RegisterRole,
        RegisterUser,
        UploadPrice,
        AuthorisedPriceApi
    }
}