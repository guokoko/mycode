using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;

namespace TestUtility
{
    public class ScheduleStorageMock : IScheduleStorage
    {
        public List<Schedule> schedules = new List<Schedule>();

        public ScheduleStorageMock(List<Schedule> initialData)
        {
            schedules.AddRange(initialData);
        }

        public Task<Schedule> NewDocument(Schedule document)
        {
            schedules.Add(document);
            return Task.FromResult(document);
        }

        public Task NewDocuments(IEnumerable<Schedule> documents)
        {
            schedules.AddRange(documents);
            return Task.CompletedTask;
        }

        public Task<Schedule> UpdateDocument(Schedule document, Expression<Func<Schedule, bool>> filter)
        {
            schedules.RemoveAll(s => filter.Compile()(s));
            schedules.Add(document);
            return Task.FromResult(document);
        }

        public Task<Schedule> DeleteDocument(string identifier, Expression<Func<Schedule, bool>> filter)
        {
            schedules.RemoveAll(s => filter.Compile()(s));
            return Task.FromResult(new Schedule());
        }

        public Task<long> DeleteDocuments(string identifier, Expression<Func<Schedule, bool>> filter)
        {
            schedules.RemoveAll(s => filter.Compile()(s));
            return Task.FromResult(1L);
        }

        public Task<long> TotalScheduleCount()
        {
            throw new NotImplementedException();
        }

        public Task<Schedule[]> GetOverlappingSchedules(ScheduleKey key)
        {
            return Task.FromResult(schedules.Where(s => s.Key.Channel == key.Channel && s.Key.Store == key.Store && s.Key.Sku == key.Sku).ToArray());
        }

        public Task<Schedule?> GetSchedule(ScheduleKey key)
        {
            return Task.FromResult<Schedule?>(schedules.SingleOrDefault(s => s.Key.Equals(key)));
        }

        public IAsyncEnumerable<Schedule> GetSchedules(string channel, string store, string sku)
        {
            return schedules.ToAsyncEnumerable();
        }

        public IAsyncEnumerable<Schedule> GetPendingStartSchedules(DateTime cutoff)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<Schedule> GetPendingEndSchedules(DateTime cutoff)
        {
            throw new NotImplementedException();
        }

        public Task<long> TotalPendingStartSchedulesCount()
        {
            throw new NotImplementedException();
        }

        public Task<long> TotalPendingEndSchedulesCount()
        {
            throw new NotImplementedException();
        }

        public Task UpdateSchedules(IEnumerable<Schedule> schedules)
        {
            throw new NotImplementedException();
        }
    }
}