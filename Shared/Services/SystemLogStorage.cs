using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CTO.Price.Shared.Log;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace CTO.Price.Shared.Services
{
    [ExcludeFromCodeCoverageAttribute]
    public sealed class SystemLogStorageOption
    {
        public string ConnectionString { get; set; } = string.Empty;
    }
    public interface ISystemLogStorage : IStorage<SystemLog> {}

    public sealed class SystemLogStorage : ISystemLogStorage
    {
        readonly IMongoCollection<SystemLog> eventLogTable;

        static SystemLogStorage() {
            BsonClassMap.RegisterClassMap<SystemLog>(cm => {
                cm.AutoMap();
                cm.MapIdMember(pm => pm.Key);
            });
        }

        [ExcludeFromCodeCoverageAttribute]
        public SystemLogStorage(IOptionsMonitor<SystemLogStorageOption> storageOption) {
            var databaseName = MongoUrl.Create(storageOption.CurrentValue.ConnectionString).DatabaseName;
            var client = new MongoClient(storageOption.CurrentValue.ConnectionString);
            var db = client.GetDatabase(databaseName);
            eventLogTable = db.GetCollection<SystemLog>("SystemLog");
        }

        public SystemLogStorage(IMongoCollection<SystemLog> mongoCollection) {
            eventLogTable = mongoCollection;
        }

        public async Task<SystemLog> NewDocument(SystemLog document) => await DocumentHelper.NewMongoDocument(eventLogTable, document);
        public async Task NewDocuments(IEnumerable<SystemLog> documents) => await DocumentHelper.NewMongoDocuments(eventLogTable, documents);

        public async Task<SystemLog> UpdateDocument(SystemLog document, Expression<Func<SystemLog, bool>> filter) 
            => await DocumentHelper.UpdateMongoDocument(eventLogTable, document, filter);

        public async Task<SystemLog> DeleteDocument(string identifier, Expression<Func<SystemLog, bool>> filter) 
            => await DocumentHelper.DeleteMongoDocument(eventLogTable, identifier, filter);

        public async Task<long> DeleteDocuments(string identifier, Expression<Func<SystemLog, bool>> filter)
            => await DocumentHelper.DeleteMongoDocuments(eventLogTable, identifier, filter);
    }
}