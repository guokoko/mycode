using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Log;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CTO.Price.Admin.Services
{
    public sealed class ConfigureMongoDbIndexesOption
    {
        public string ConnectionString { get; set; } = string.Empty;
    }
    
    [ExcludeFromCodeCoverage]
    public class ConfigureMongoDbIndexesService : IHostedService
    {   
        readonly IMongoCollection<AuditLog> auditLogTable;
        readonly IMongoCollection<EventLog> priceUpdateTable;
        readonly IMongoCollection<SystemLog> systemLogTable;
        readonly ExpireAfter expireAfter;

        public ConfigureMongoDbIndexesService(IOptionsMonitor<ConfigureMongoDbIndexesOption> storageOption, IOptionsMonitor<LoggerRetentionDurationSetting> loggerRetentionDurationSetting)
        {
            expireAfter = loggerRetentionDurationSetting.CurrentValue.ExpireAfter;
            var databaseName = MongoUrl.Create(storageOption.CurrentValue.ConnectionString).DatabaseName;
            var client = new MongoClient(storageOption.CurrentValue.ConnectionString);
            var db = client.GetDatabase(databaseName);
            
            auditLogTable = db.GetCollection<AuditLog>("AuditLog");
            priceUpdateTable = db.GetCollection<EventLog>("EventAuditLog");
            systemLogTable = db.GetCollection<SystemLog>("SystemLog");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var indexKeysDefinitionAuditLog = Builders<AuditLog>.IndexKeys.Ascending(x => x.LogTime);
            var auditLoMmodel = new CreateIndexModel<AuditLog>(indexKeysDefinitionAuditLog, new CreateIndexOptions() { ExpireAfter = new TimeSpan(expireAfter.Days, expireAfter.Hours, expireAfter.Minutes, expireAfter.Seconds), Name = "ExpireAt"});
            await auditLogTable.Indexes.CreateOneAsync(auditLoMmodel, cancellationToken: cancellationToken);
            
            var indexKeysDefinitionEventLog = Builders<EventLog>.IndexKeys.Ascending(x => x.Timestamp);
            var eventLogModel = new CreateIndexModel<EventLog>(indexKeysDefinitionEventLog, new CreateIndexOptions() { ExpireAfter = new TimeSpan(expireAfter.Days, expireAfter.Hours, expireAfter.Minutes, expireAfter.Seconds), Name = "ExpireAt"});
            await priceUpdateTable.Indexes.CreateOneAsync(eventLogModel, cancellationToken: cancellationToken);
            
            var indexKeysDefinitionSystemLog = Builders<SystemLog>.IndexKeys.Ascending(x => x.Timestamp);
            var systemLogModel = new CreateIndexModel<SystemLog>(indexKeysDefinitionSystemLog, new CreateIndexOptions() { ExpireAfter = new TimeSpan(expireAfter.Days, expireAfter.Hours, expireAfter.Minutes, expireAfter.Seconds), Name = "ExpireAt"});
            await systemLogTable.Indexes.CreateOneAsync(systemLogModel, cancellationToken: cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}