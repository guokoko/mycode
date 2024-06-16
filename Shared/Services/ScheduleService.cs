using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Enums;
using static RZ.Foundation.Prelude;
// ReSharper disable AccessToModifiedClosure

namespace CTO.Price.Shared.Services
{
    public interface IScheduleService
    {
        Task<long> TotalScheduleCount();
        IAsyncEnumerable<Schedule> GetSchedules(string channel, string store, string sku);
        IAsyncEnumerable<Schedule> GetPendingStartSchedules(DateTime cutoff);
        IAsyncEnumerable<Schedule> GetPendingEndSchedules(DateTime cutoff);
        Task<long> TotalPendingStartSchedulesCount();
        Task<long> TotalPendingEndSchedulesCount();
        Task UpdateSchedules(IEnumerable<Schedule> schedules);
        Task UpdateSchedule(PriceModel incoming, DateTime now);
        Task<UpdateResult> DeleteSchedule(ScheduleKey key);
    }
    
    public class ScheduleService : IScheduleService
    {
        readonly IScheduleStorage scheduleStorage;
        readonly IEventLogService eventLogger;
        
        public ScheduleService(IScheduleStorage scheduleStorage, IEventLogService eventLogger)
        {
            this.scheduleStorage = scheduleStorage;
            this.eventLogger = eventLogger;
        }

        public IAsyncEnumerable<Schedule> GetSchedules(string channel, string store, string sku) => scheduleStorage.GetSchedules(channel, store, sku);
        public IAsyncEnumerable<Schedule> GetPendingStartSchedules(DateTime cutoff) => scheduleStorage.GetPendingStartSchedules(cutoff);
        public IAsyncEnumerable<Schedule> GetPendingEndSchedules(DateTime cutoff) => scheduleStorage.GetPendingEndSchedules(cutoff);
        public async Task<long> TotalPendingStartSchedulesCount() => await scheduleStorage.TotalPendingStartSchedulesCount();
        public async Task<long> TotalPendingEndSchedulesCount() => await scheduleStorage.TotalPendingEndSchedulesCount();
        public async Task<long> TotalScheduleCount() => await scheduleStorage.TotalScheduleCount();

        public async Task<UpdateResult> DeleteSchedule(ScheduleKey key)
        {
            Retry:
            var result = await TryAsync(() => DocumentHelper.TryDelete(() => scheduleStorage.DeleteDocument(key.ToString(), s => s.Key == key && s.Status == ScheduleStatus.PendingStart),
                UpdateState.Ignore)).Try();
            
            switch (result.GetOrElse(_ => UpdateState.NeedRetry))
            {
                case UpdateState.NeedRetry:
                    goto Retry;
                case UpdateState.Ignore:
                    throw new ConstraintException($"Schedule {key} can not be deleted. Only inactive schedules can be deleted.");
                case UpdateState.Deleted:
                    return UpdateResult.Deleted;
                default: throw new Exception("Delete state can only be one of the three status, NeedRetry, Ignore, or, Deleted.");
            }
        }

        public async Task UpdateSchedules(IEnumerable<Schedule> schedules) => await scheduleStorage.UpdateSchedules(schedules);

        public async Task UpdateSchedule(PriceModel incoming, DateTime now)
        {
            var schedules = incoming.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(PriceDescription) && p.Name != "NormalPrice")
                .Select(p => Optional((PriceDescription?) p.GetValue(incoming)).Map(pd => new {pd.Start, pd.End, p.Name, pd}))
                .Where(pd => pd.IsSome)
                .Select(pd => pd.Get())
                .Where(pd =>
                    !(pd.Start == null && pd.End == null) &&
                    (pd.Start == null ||
                     (pd.Start != null && ((pd.Start < now && pd.End != null) || pd.Start > now))) &&
                    (pd.End == null) || (pd.End != null && pd.End > now))
                .GroupBy(pd => new {pd.Start, pd.End})
                .ToArray();
            
            
            // For Retail Price (base price), only promotion price is allowed to have scheduling
            // For Channel Price, only original price is used
            // Therefore, there can only be one scheduling per message. This was known just before One JDA project 2020
            // We did not have enough time to refactor the whole thing.
            
            if (schedules.Length() == 1)
            {
                var schedule = schedules.First();
                
                Retry:
                var incomingSchedule = new Schedule()
                {
                    Key = new ScheduleKey(schedule.Key.Start, schedule.Key.End, incoming.Key.Channel, incoming.Key.Store, incoming.Key.Sku),
                    PriceUpdate = new SchedulePriceUpdate()
                    {
                        OriginalPrice = schedule.SingleOrDefault(p => p.Name == nameof(PriceModel.OriginalPrice))?.pd,
                        SalePrice = schedule.SingleOrDefault(p => p.Name == nameof(PriceModel.SalePrice))?.pd,
                        PromotionPrice = schedule.SingleOrDefault(p => p.Name == nameof(PriceModel.PromotionPrice))?.pd,
                        AdditionalData = incoming.AdditionalData
                    },
                    Status = schedule.Key.Start > now ? ScheduleStatus.PendingStart : ScheduleStatus.PendingEnd,
                    LastUpdate = now
                };
                
                await TryAsync(async () => await scheduleStorage.DeleteDocument(incomingSchedule.Key.ToString(),
                    s => s.Key.Channel == incomingSchedule.Key.Channel &&
                         s.Key.Store == incomingSchedule.Key.Store &&
                         s.Key.Sku == incomingSchedule.Key.Sku
                )).Try();
                
                var storageResult = await TryAsync(() => DocumentHelper.TryAddNew(() => scheduleStorage.NewDocument(incomingSchedule))).Try();

                switch (storageResult.GetOrElse(_ => UpdateState.NeedRetry))
                {
                    case UpdateState.NeedRetry:
                        goto Retry;
                    case UpdateState.Updated:
                        await eventLogger.Information(incomingSchedule.Key.Channel, incomingSchedule.Key.Store, incomingSchedule.Key.Sku, EventEnum.UpdateSchedule, incomingSchedule);
                        break;
                    default:
                        await eventLogger.Error(incomingSchedule.Key.Channel, incomingSchedule.Key.Store, incomingSchedule.Key.Sku, EventEnum.UpdateSchedule, incomingSchedule);
                        throw new NotSupportedException($"Unrecognized UpdateResult = {storageResult}");
                }
            }
            else
            {
                await eventLogger.Error(incoming.Key, EventEnum.InvalidInput, schedules);
            }
        }
    }
}