using System.Linq;
using CTO.Price.Shared.Domain;
using Mongo.Migration.Migrations.Database;
using MongoDB.Driver;
using RZ.Foundation;

namespace Price.MongoMigrations
{
    public class M0001_20201110_AddExtraAttributeToId : DatabaseMigration
    {
        public M0001_20201110_AddExtraAttributeToId() : base("1.0.0") { }
        private const string CurrentPhysicalStoreString = "PhysicalStore";
        private const string PriceUpdateCollectionName = "PriceUpdate";
        
        
        // Since key is the ID of the document, simply updating the document is not possible
        
        public override void Up(IMongoDatabase db)
        {
            var collection = db.GetCollection<PriceModel>(PriceUpdateCollectionName);
            var all = collection.Find(d => d.Key.Channel == CurrentPhysicalStoreString).ToListAsync().Result;
            var allCopy = all.Select(d => d.SideEffect(s => s.Key = new PriceModelKey(null, s.Key.Store, s.Key.Sku))).ToArray();
            
            if (allCopy.Length > 0)
                collection.InsertMany(allCopy);

            collection.DeleteMany(d => d.Key.Channel == CurrentPhysicalStoreString);
            
        }

        public override void Down(IMongoDatabase db)
        {
            var collection = db.GetCollection<PriceModel>(PriceUpdateCollectionName);
            var all = collection.Find(d => d.Key.Channel == null).ToListAsync().Result;
            var allCopy = all.Select(d => d.SideEffect(s => s.Key = new PriceModelKey(CurrentPhysicalStoreString, s.Key.Store, s.Key.Sku))).ToArray();
            
            if (allCopy.Length > 0)
                collection.InsertMany(allCopy);
            
            collection.DeleteMany(d => d.Key.Channel == null);
        }
    }
}