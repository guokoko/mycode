using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CTO.Price.Admin.Data
{
    public class User
    {
        [BsonId]
        public string Email { get; set; } = null!;
        
        public string Role { get; set; } = null!;
        public DateTime LastUpdate { get; set; }
    }
}