using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Extensions;
using CTO.Price.Shared.Log;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using RZ.Foundation.Extensions;
using static RZ.Foundation.Prelude;

namespace CTO.Price.Shared.Services
{
    [ExcludeFromCodeCoverageAttribute]
    public sealed class EventLogStorageOption
    {
        public string ConnectionString { get; set; } = string.Empty;
    }
    public interface IEventLogStorage : IStorage<EventLog> {}

    public sealed class EventLogStorage : IEventLogStorage
    {
        readonly IMongoCollection<EventLog> eventLogTable;

        static EventLogStorage() {
            BsonClassMap.RegisterClassMap<EventLog>(cm => {
                cm.AutoMap();
                cm.MapIdMember(pm => pm.Key);
            });
        }

        [ExcludeFromCodeCoverageAttribute]
        public EventLogStorage(IOptionsMonitor<EventLogStorageOption> storageOption) {
            var databaseName = MongoUrl.Create(storageOption.CurrentValue.ConnectionString).DatabaseName;
            var client = new MongoClient(storageOption.CurrentValue.ConnectionString);
            var db = client.GetDatabase(databaseName);
            eventLogTable = db.GetCollection<EventLog>("EventAuditLog");
        }

        public EventLogStorage(IMongoCollection<EventLog> mongoCollection) {
            eventLogTable = mongoCollection;
        }

        public async Task<EventLog> NewDocument(EventLog document) => await DocumentHelper.NewMongoDocument(eventLogTable, document);
        public async Task NewDocuments(IEnumerable<EventLog> documents) => await DocumentHelper.NewMongoDocuments(eventLogTable, documents);

        public async Task<EventLog> UpdateDocument(EventLog document, Expression<Func<EventLog, bool>> filter) 
            => await DocumentHelper.UpdateMongoDocument(eventLogTable, document, filter);

        public async Task<EventLog> DeleteDocument(string identifier, Expression<Func<EventLog, bool>> filter) 
            => await DocumentHelper.DeleteMongoDocument(eventLogTable, identifier, filter);

        public async Task<long> DeleteDocuments(string identifier, Expression<Func<EventLog, bool>> filter)
            => await DocumentHelper.DeleteMongoDocuments(eventLogTable, identifier, filter);
    }
}