using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Shared;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CTO.Price.Admin.Services
{
    [ExcludeFromCodeCoverageAttribute]
    public sealed class UploadLogStorageOption
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public interface IUploadLogStorage : IStorage<UploadLog>
    {
        Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate, string fileName, UploadResult? result, string email);
        Task<List<UploadLog>> GetUploadLogWithFilter(DateTime fromDate, DateTime endDate, string fileName,
            UploadResult? result, string email, int pageIndex, int pageSize);
    }
    
    public class UploadLogStorage : IUploadLogStorage
    {
        readonly IMongoCollection<UploadLog> uploadLogTable;

        [ExcludeFromCodeCoverageAttribute]
        public UploadLogStorage(IOptionsMonitor<UploadLogStorageOption> storageOption)
        {
            var databaseName = MongoUrl.Create(storageOption.CurrentValue.ConnectionString).DatabaseName;
            var client = new MongoClient(storageOption.CurrentValue.ConnectionString);
            var db = client.GetDatabase(databaseName);
            uploadLogTable = db.GetCollection<UploadLog>("UploadLog");
        }

        public UploadLogStorage(IMongoCollection<UploadLog> mongoCollection) {
            uploadLogTable = mongoCollection;
        }
        
        public async Task<UploadLog> NewDocument(UploadLog document) => await DocumentHelper.NewMongoDocument(uploadLogTable, document);

        public async Task NewDocuments(IEnumerable<UploadLog> documents) => await DocumentHelper.NewMongoDocuments(uploadLogTable, documents);

        public async Task<UploadLog> UpdateDocument(UploadLog document, Expression<Func<UploadLog, bool>> filter) 
            => await DocumentHelper.UpdateMongoDocument(uploadLogTable, document, filter);

        public async Task<UploadLog> DeleteDocument(string identifier, Expression<Func<UploadLog, bool>> filter)
            => await DocumentHelper.DeleteMongoDocument(uploadLogTable, identifier, filter);

        public async Task<long> DeleteDocuments(string identifier, Expression<Func<UploadLog, bool>> filter)
            => await DocumentHelper.DeleteMongoDocuments(uploadLogTable, identifier, filter);
        
        public async Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate,
            string fileName, UploadResult? result, string email)
            => await uploadLogTable.CountDocumentsAsync(l => 
                l.LogTime >= fromDate.Date && l.LogTime < endDate.Date.AddDays(1)
                                           && (string.IsNullOrWhiteSpace(fileName) || l.FileName == fileName)
                                           && (result == null || l.Result == result)
                                           && (string.IsNullOrEmpty(email) || l.Email.ToLower().Contains(email.ToLower())));
        
        public async Task<List<UploadLog>> GetUploadLogWithFilter(DateTime fromDate, DateTime endDate,
            string fileName, UploadResult? result, string email, int pageIndex, int pageSize)
            => await uploadLogTable.Find(l => 
                l.LogTime >= fromDate.Date && l.LogTime < endDate.Date.AddDays(1) 
                                           && (string.IsNullOrWhiteSpace(fileName) || l.FileName == fileName)
                                           && (result == null || l.Result == result)
                                           && (string.IsNullOrEmpty(email) || l.Email.ToLower().Contains(email.ToLower()))
            ).SortByDescending( o => o.LogTime).Skip((pageIndex - 1) * pageSize).Limit(pageSize).ToListAsync();
    }
}