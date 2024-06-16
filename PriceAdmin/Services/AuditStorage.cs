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
    public sealed class AuditStorageOption
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public interface IAuditStorage : IStorage<AuditLog>
    {
        Task<List<AuditLog>> GetLogByEmail(string email);
        Task InsertLog(AuditLog log);
        
        Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate, AuditLogActionType actionType, string email);

        Task<List<AuditLog>> GetAuditLogWithFilter(DateTime fromDate, DateTime endDate, AuditLogActionType actionType, string email, int pageIndex, int pageSize);
    }
    
    public class AuditStorage : IAuditStorage
    {
        readonly IMongoCollection<AuditLog> auditTable;

        [ExcludeFromCodeCoverageAttribute]
        public AuditStorage(IOptionsMonitor<AuditStorageOption> storageOption) {
            var databaseName = MongoUrl.Create(storageOption.CurrentValue.ConnectionString).DatabaseName;
            var client = new MongoClient(storageOption.CurrentValue.ConnectionString);
            var db = client.GetDatabase(databaseName);
            auditTable = db.GetCollection<AuditLog>("AuditLog");
        }

        public AuditStorage(IMongoCollection<AuditLog> mongoCollection) {
            auditTable = mongoCollection;
        }

        public async Task<List<AuditLog>> GetLogByEmail(string email) =>
            await (await auditTable.FindAsync(a => a.Email == email)).ToListAsync();

        public async Task InsertLog(AuditLog log) {
            await auditTable.InsertOneAsync(log);
        }
        
        public async Task<AuditLog> NewDocument(AuditLog document) => await DocumentHelper.NewMongoDocument(auditTable, document);
        public async Task NewDocuments(IEnumerable<AuditLog> documents) => await DocumentHelper.NewMongoDocuments(auditTable, documents);

        public async Task<AuditLog> UpdateDocument(AuditLog document, Expression<Func<AuditLog, bool>> filter) 
            => await DocumentHelper.UpdateMongoDocument(auditTable, document, filter);

        public async Task<AuditLog> DeleteDocument(string identifier, Expression<Func<AuditLog, bool>> filter) 
            => await DocumentHelper.DeleteMongoDocument(auditTable, identifier, filter);

        public async Task<long> DeleteDocuments(string identifier, Expression<Func<AuditLog, bool>> filter)
            => await DocumentHelper.DeleteMongoDocuments(auditTable, identifier, filter);

        public async Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate,
            AuditLogActionType actionType, string email)
        => await auditTable.CountDocumentsAsync(l =>
                l.LogTime >= fromDate.Date && l.LogTime < endDate.Date.AddDays(1)
                                           && (actionType == AuditLogActionType.All || l.Action == actionType)
                                           && (string.IsNullOrEmpty(email) || l.Email.ToLower().Contains(email.ToLower())));
        

        public async Task<List<AuditLog>> GetAuditLogWithFilter(DateTime fromDate, DateTime endDate,
            AuditLogActionType actionType, string email, int pageIndex, int pageSize)
        => await auditTable.Find(l =>
                            l.LogTime >= fromDate.Date && l.LogTime < endDate.Date.AddDays(1)
                                           && (actionType == AuditLogActionType.All || l.Action == actionType)
                                           && (string.IsNullOrEmpty(email) || l.Email.ToLower().Contains(email.ToLower()))
                                        ).SortByDescending( o => o.LogTime).Skip((pageIndex - 1) * pageSize).Limit(pageSize).ToListAsync();
    }
}