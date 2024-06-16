using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CTO.Price.Proto.V1;
using CTO.Price.Protos;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Log;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace CTO.Price.Admin.Services
{
    [ExcludeFromCodeCoverageAttribute]
    public sealed class PriceEventLogStorageOption
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public interface IPriceEventLogStorage
    {
        Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate, EventEnum? eventEnum,
            LogLevelEnum? logLevelEnum,
            string channel, string store, string sku);

        Task<List<EventLog>> GetPriceEventLogWithFilter(DateTime fromDate, DateTime endDate, EventEnum? eventEnum,
            LogLevelEnum? logLevelEnum,
            string channel, string store, string sku, int pageIndex, int pageSize);
    }

    public class PriceEventLogStorage : IPriceEventLogStorage
    {
        readonly IMongoCollection<EventLog> priceUpdateTable;

        static PriceEventLogStorage()
        {
            BsonClassMap.RegisterClassMap<PriceModel>(cm => { cm.AutoMap(); });
            BsonClassMap.RegisterClassMap<Schedule>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(pm => pm.Key);
            });
            BsonClassMap.RegisterClassMap<GetPricesReply>();
            BsonClassMap.RegisterClassMap<RawPrice>();
        }

        [ExcludeFromCodeCoverageAttribute]
        public PriceEventLogStorage(IOptionsMonitor<PriceEventLogStorageOption> storageOption)
        {
            var databaseName = MongoUrl.Create(storageOption.CurrentValue.ConnectionString).DatabaseName;
            var client = new MongoClient(storageOption.CurrentValue.ConnectionString);
            var db = client.GetDatabase(databaseName);

            priceUpdateTable = db.GetCollection<EventLog>("EventAuditLog");
        }

        public PriceEventLogStorage(IMongoCollection<EventLog> mongoCollection) {
            priceUpdateTable = mongoCollection;
        }

        public async Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate,
            EventEnum? eventEnum, LogLevelEnum? logLevelEnum, string channel, string store, string sku) =>
            await priceUpdateTable.CountDocumentsAsync(l =>
                l.Timestamp >= fromDate && l.Timestamp < endDate
                                             && (eventEnum == null || l.Event == eventEnum)
                                             && (logLevelEnum == null || l.Level == logLevelEnum)
                                             && (string.IsNullOrEmpty(channel) || l.Identifier.Channel.ToLower().Contains(channel.ToLower()))
                                             && (string.IsNullOrEmpty(store) || l.Identifier.Store.ToLower().Contains(store.ToLower()))
                                             && (string.IsNullOrEmpty(sku) || l.Identifier.Sku.ToLower().Contains(sku.ToLower())));

        public async Task<List<EventLog>> GetPriceEventLogWithFilter(DateTime fromDate, DateTime endDate,
            EventEnum? eventEnum, LogLevelEnum? logLevelEnum, string channel, string store, string sku, int pageIndex, int pageSize) => await priceUpdateTable.Find(l =>
                l.Timestamp >= fromDate && l.Timestamp < endDate
                                             && (eventEnum == null || l.Event == eventEnum)
                                             && (logLevelEnum == null || l.Level == logLevelEnum)
                                             && (string.IsNullOrEmpty(channel) || l.Identifier.Channel.ToLower().Contains(channel.ToLower()))
                                             && (string.IsNullOrEmpty(store) || l.Identifier.Store.ToLower().Contains(store.ToLower()))
                                             && (string.IsNullOrEmpty(sku) || l.Identifier.Sku.ToLower().Contains(sku.ToLower()))
                                             ).SortByDescending(o => o.Timestamp).Skip((pageIndex - 1) * pageSize).Limit(pageSize).ToListAsync();

    }
}