using System;
using System.Collections;
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
    public sealed class ScheduleStorageOption
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public interface IScheduleStorage : IStorage<Schedule>
    {
        Task<long> TotalScheduleCount();
        Task<Schedule[]> GetOverlappingSchedules(ScheduleKey key);
        Task<Schedule?> GetSchedule(ScheduleKey key);
        IAsyncEnumerable<Schedule> GetSchedules(string channel, string store, string sku);
        IAsyncEnumerable<Schedule> GetPendingStartSchedules(DateTime cutoff);
        IAsyncEnumerable<Schedule> GetPendingEndSchedules(DateTime cutoff);
        Task<long> TotalPendingStartSchedulesCount();
        Task<long> TotalPendingEndSchedulesCount();
        Task UpdateSchedules(IEnumerable<Schedule> schedules);
    }
    
    public class ScheduleStorage : IScheduleStorage
    {
        readonly IMongoCollection<Schedule> scheduleTable;
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        static ScheduleStorage() {
            BsonClassMap.RegisterClassMap<Schedule>(cm => {
                cm.AutoMap();
                cm.MapIdMember(pm => pm.Key);
            });
        }

        [ExcludeFromCodeCoverageAttribute]
        public ScheduleStorage(IOptionsMonitor<ScheduleStorageOption> storageOption) {
            var databaseName = MongoUrl.Create(storageOption.CurrentValue.ConnectionString).DatabaseName;
            var client = new MongoClient(storageOption.CurrentValue.ConnectionString);
            var db = client.GetDatabase(databaseName);
            scheduleTable = db.GetCollection<Schedule>("Schedule");
        }

        public ScheduleStorage(IMongoCollection<Schedule> mongoCollection) {
            scheduleTable = mongoCollection;
        }

        public async Task<long> TotalScheduleCount() => await scheduleTable.EstimatedDocumentCountAsync();

        public async Task<Schedule[]> GetOverlappingSchedules(ScheduleKey key) => 
            (await scheduleTable.FindAsync(s => s.Key.Channel == key.Channel && s.Key.Store == key.Store && s.Key.Sku == key.Sku)).ToList().ToArray();
        
        public async Task<Schedule?> GetSchedule(ScheduleKey key) =>
            await (await scheduleTable.FindAsync(s => s.Key == key)).SingleOrDefaultAsync();

        public async IAsyncEnumerable<Schedule> GetSchedules(string channel, string store, string sku)
        {
            var cursor = await scheduleTable.FindAsync(s => s.Key.Channel == channel && s.Key.Store == store && s.Key.Sku == sku);
            while (await cursor.MoveNextAsync())
            {
                foreach (var schedule in cursor.Current)
                {
                    yield return schedule;
                }
            }
        }

        public async IAsyncEnumerable<Schedule> GetPendingStartSchedules(DateTime cutoff)
        {
            var cursor = await scheduleTable.FindAsync(s => s.Status == ScheduleStatus.PendingStart && s.Key.StartDate < cutoff);
            while (await cursor.MoveNextAsync())
            {
                foreach (var schedule in cursor.Current)
                {
                    yield return schedule;
                }
            }
        }
        
        public async IAsyncEnumerable<Schedule> GetPendingEndSchedules(DateTime cutoff)
        {
            var cursor = await scheduleTable.FindAsync(s => s.Status == ScheduleStatus.PendingEnd && s.Key.EndDate < cutoff);
            while (await cursor.MoveNextAsync())
            {
                foreach (var schedule in cursor.Current)
                {
                    yield return schedule;
                }
            }
        }

        public async Task<long> TotalPendingStartSchedulesCount() =>
            await scheduleTable.CountDocumentsAsync(s => s.Status == ScheduleStatus.PendingStart);

        public async Task<long> TotalPendingEndSchedulesCount() => 
            await scheduleTable.CountDocumentsAsync(s => s.Status == ScheduleStatus.PendingEnd);

        public async Task<Schedule> NewDocument(Schedule document)
        {
            Schedule aSchedule = null;
            await semaphoreSlim.WaitAsync();
            try
            {
                aSchedule = await DocumentHelper.NewMongoDocument(scheduleTable, document);
            }
            finally
            {
                semaphoreSlim.Release();
            }
            return aSchedule;
        }
        public async Task NewDocuments(IEnumerable<Schedule> documents)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await DocumentHelper.NewMongoDocuments(scheduleTable, documents);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task<Schedule> UpdateDocument(Schedule schedule, Expression<Func<Schedule, bool>> filter)
        {
            Schedule aSchedule = null;
            await semaphoreSlim.WaitAsync();
            try
            {
                aSchedule = await DocumentHelper.UpdateMongoDocument(scheduleTable, schedule, filter);
            }
            finally
            {
                semaphoreSlim.Release();
            }
            return aSchedule;
        }

        public async Task<Schedule> DeleteDocument(string identifier, Expression<Func<Schedule, bool>> filter)
        {
            Schedule aSchedule = null;
            await semaphoreSlim.WaitAsync();
            try
            {
                aSchedule = await DocumentHelper.DeleteMongoDocument(scheduleTable, identifier, filter);
            }
            finally
            {
                semaphoreSlim.Release();
            }
            return aSchedule;
        }
        public async Task<long> DeleteDocuments(string identifier, Expression<Func<Schedule, bool>> filter)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return await DocumentHelper.DeleteMongoDocuments(scheduleTable, identifier, filter);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task UpdateSchedules(IEnumerable<Schedule> schedules)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                if (!schedules.Any())
                    return;

                var scheduleDict = new Dictionary<ScheduleKey, Schedule>();
                foreach (var schedule in schedules)
                {
                    if (scheduleDict.TryGetValue(schedule.Key, out var existingSchedule))
                    {
                        if (schedule.Key.EndDate > existingSchedule.Key.EndDate)
                        {
                            scheduleDict[schedule.Key] = schedule;
                        }
                    }
                    else
                    {
                        scheduleDict[schedule.Key] = schedule;
                    }
                }

                var bulkOps = new List<WriteModel<Schedule>>();
                foreach (var schedule in scheduleDict.Values)
                {
                    var upsertOne = new ReplaceOneModel<Schedule>(Builders<Schedule>.Filter.Where(x => x.Key == schedule.Key && x.LastUpdate == schedule.LastUpdate), schedule)
                    {
                        IsUpsert = true
                    };
                    bulkOps.Add(upsertOne);
                }

                var result = await scheduleTable.BulkWriteAsync(bulkOps);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}