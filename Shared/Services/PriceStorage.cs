using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CTO.Price.Shared.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using RZ.Foundation;

namespace CTO.Price.Shared.Services
{
    [ExcludeFromCodeCoverageAttribute]
    public sealed class PriceStorageOption
    {
        public string ConnectionString { get; set; } = string.Empty;
    }
    public interface IPriceStorage : IStorage<PriceModel>
    {
        Task<Option<PriceModel>> GetPrice(PriceModelKey key);
        Task<PriceModel[]> GetPrices(IEnumerable<PriceModelKey> keys);
        Task<IEnumerable<PriceModelKey>> GetPriceModelKeys(string sku);
        Task<long> TotalPriceCount();
        Task<PriceModel> UpdateDocumentThrow(PriceModel document, Expression<Func<PriceModel, bool>> filter);

        Task<PriceModel> NewMongoDocumentReplace(PriceModel document);
    }

    public sealed class PriceStorage : IPriceStorage
    {
        readonly IMongoCollection<PriceModel> priceTable;
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        static PriceStorage() {
            BsonClassMap.RegisterClassMap<PriceModel>(cm => {
                cm.AutoMap();
                cm.MapIdMember(pm => pm.Key);
            });
        }

        [ExcludeFromCodeCoverageAttribute]
        public PriceStorage(IOptionsMonitor<PriceStorageOption> storageOption) {
            var databaseName = MongoUrl.Create(storageOption.CurrentValue.ConnectionString).DatabaseName;
            var client = new MongoClient(storageOption.CurrentValue.ConnectionString);
            var db = client.GetDatabase(databaseName);
            priceTable = db.GetCollection<PriceModel>("PriceUpdate");
        }

        public PriceStorage(IMongoCollection<PriceModel> mongoCollection) {
            priceTable = mongoCollection;
        }

        public async Task<Option<PriceModel>> GetPrice(PriceModelKey key) =>
            await (await priceTable.FindAsync(p => p.Key == key)).SingleOrDefaultAsync();

        public async Task<PriceModel[]> GetPrices(IEnumerable<PriceModelKey> keys) =>
            (await (await priceTable.FindAsync(p => keys.Contains(p.Key))).ToListAsync()).ToArray();

        public async Task<IEnumerable<PriceModelKey>> GetPriceModelKeys(string sku) => 
            (await (await priceTable.FindAsync(p => p.Key.Sku.Equals(sku))).ToListAsync()).ToArray().Select(c => c.Key);

        public async Task<long> TotalPriceCount() => await priceTable.EstimatedDocumentCountAsync();
        
        public async Task<PriceModel> NewDocument(PriceModel document)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return await DocumentHelper.NewMongoDocument(priceTable, document);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task NewDocuments(IEnumerable<PriceModel> documents)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await DocumentHelper.NewMongoDocuments(priceTable, documents);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
        public async Task<PriceModel> UpdateDocument(PriceModel document, Expression<Func<PriceModel, bool>> filter)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return await DocumentHelper.UpdateMongoDocument(priceTable, document, filter);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task<PriceModel> DeleteDocument(string identifier, Expression<Func<PriceModel, bool>> filter)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return await DocumentHelper.DeleteMongoDocument(priceTable, identifier, filter);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task<long> DeleteDocuments(string identifier, Expression<Func<PriceModel, bool>> filter)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return await DocumentHelper.DeleteMongoDocuments(priceTable, identifier, filter);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task<PriceModel> UpdateDocumentThrow(PriceModel document, Expression<Func<PriceModel, bool>> filter)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return await DocumentHelper.UpdateMongoDocumentThrow(priceTable, document, filter);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
        public async Task<PriceModel> NewMongoDocumentReplace(PriceModel document)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return await DocumentHelper.NewMongoDocumentReplace(priceTable, document);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

    }
}