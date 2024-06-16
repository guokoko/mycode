using Mongo.Migration.Migrations.Database;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Price.MongoMigrations
{
    public class M0003_20201111_AddExtraFieldToAuditLog : DatabaseMigration
    {
        public M0003_20201111_AddExtraFieldToAuditLog() : base("1.0.2") { }
        private const string AuditLogCollectionName = "AuditLog";
        private const string IdBsonElementName = "_id";
        private const string SystemUsingApiBsonElementName = "SystemUsingApi";

        public override void Up(IMongoDatabase db)
        {
            var collection = db.GetCollection<BsonDocument>(AuditLogCollectionName);
            var inserted = collection.Find(x => true).ToList();
             
            foreach (var item in inserted)
            {
                item.Add(new BsonElement(SystemUsingApiBsonElementName, string.Empty));
                collection.ReplaceOne(new BsonDocument(IdBsonElementName, item[IdBsonElementName]), item);
            }
        }

        public override void Down(IMongoDatabase db)
        {
            var collection = db.GetCollection<BsonDocument>(AuditLogCollectionName);
            var inserted = collection.Find(x => true).ToList();
            
            foreach (var item in inserted)
            {
                item.RemoveElement(item.GetElement(SystemUsingApiBsonElementName));
                collection.ReplaceOne(new BsonDocument(IdBsonElementName, item[IdBsonElementName]), item);
            }
        }
    }
}