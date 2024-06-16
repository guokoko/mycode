using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CTO.Price.Admin.Data
{
    public class UserRole
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;
        
        public string Role { get; set; } = string.Empty;
        
        [BsonRepresentation(BsonType.String)]
        public RolePolicy[] Policy { get; set; } = null!;

        public const string Anonymous = "Anonymous";
    }
}