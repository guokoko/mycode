using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Log;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CTO.Price.Admin.Services
{
    [ExcludeFromCodeCoverageAttribute]
    public sealed class SystemLogStorageOption
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public interface ISystemLogStorage
    {
        Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate, string projectName, LogLevelEnum? level,
            string message);

        Task<List<SystemLog>> GetSystemLogWithFilter(DateTime fromDate, DateTime endDate, string projectName, LogLevelEnum? level,
            string message, int pageIndex, int pageSize);
    }

    public class SystemLogStorage : ISystemLogStorage
    {
        readonly IMongoCollection<SystemLog> systemLogTable;

        [ExcludeFromCodeCoverageAttribute]
        public SystemLogStorage(IOptionsMonitor<SystemLogStorageOption> storageOption)
        {
            var databaseName = MongoUrl.Create(storageOption.CurrentValue.ConnectionString).DatabaseName;
            var client = new MongoClient(storageOption.CurrentValue.ConnectionString);
            var db = client.GetDatabase(databaseName);
            
            systemLogTable = db.GetCollection<SystemLog>("SystemLog");
        }

        public SystemLogStorage(IMongoCollection<SystemLog> mongoCollection) {
            systemLogTable = mongoCollection;
        }

        public async Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate, string projectName, LogLevelEnum? level,
            string message) => await systemLogTable.CountDocumentsAsync(l =>
                                            l.Timestamp >= fromDate && l.Timestamp < endDate
                                             && (level == null || l.Level == level)
                                             && (string.IsNullOrEmpty(projectName) || l.ProjectName.ToLower().Contains(projectName.ToLower()))
                                             && (string.IsNullOrEmpty(message) || l.Message.ToLower().Contains(message.ToLower())));

        public async Task<List<SystemLog>> GetSystemLogWithFilter(DateTime fromDate, DateTime endDate, string projectName, LogLevelEnum? level,
            string message, int pageIndex, int pageSize) => await systemLogTable.Find(l =>
                l.Timestamp >= fromDate && l.Timestamp < endDate
                                             && (level == null || l.Level == level)
                                             && (string.IsNullOrEmpty(projectName) || l.ProjectName.ToLower().Contains(projectName.ToLower()))
                                             && (string.IsNullOrEmpty(message) || l.Message.ToLower().Contains(message.ToLower()))
                                             ).SortByDescending(o => o.Timestamp).Skip((pageIndex - 1) * pageSize).Limit(pageSize).ToListAsync();
    }
}