using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CTO.Price.Shared.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace CTO.Price.Shared.Services
{
    [ExcludeFromCodeCoverageAttribute]
    public sealed class DeleteSkuStorageOption
    {
        public string ConnectionString { get; set; } = string.Empty;
    }
    public interface IDeleteSkuStorage
    {
        Task<DeletedSKUsModel> NewDocument(DeletedSKUsModel document);
    }

    public sealed class DeleteSkuStorage : IDeleteSkuStorage
    {
        readonly IMongoCollection<DeletedSKUsModel> deletedSkusTable;

        static DeleteSkuStorage() {
            BsonClassMap.RegisterClassMap<DeletedSKUsModel>(cm => {
                cm.AutoMap();
                cm.MapIdMember(pm => pm.Key);
            });
        }

        [ExcludeFromCodeCoverageAttribute]
        public DeleteSkuStorage(IOptionsMonitor<DeleteSkuStorageOption> storageOption) {
            var databaseName = MongoUrl.Create(storageOption.CurrentValue.ConnectionString).DatabaseName;
            var client = new MongoClient(storageOption.CurrentValue.ConnectionString);
            var db = client.GetDatabase(databaseName);
            deletedSkusTable = db.GetCollection<DeletedSKUsModel>("DeletedSkus");
        }

        public DeleteSkuStorage(IMongoCollection<DeletedSKUsModel> mongoCollection) {
            deletedSkusTable = mongoCollection;
        }

        public async Task<DeletedSKUsModel> NewDocument(DeletedSKUsModel document) => await DocumentHelper.NewMongoDocument(deletedSkusTable, document);

    }
}