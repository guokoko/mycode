using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Log;

namespace CTO.Price.Admin.Services
{
    public interface IPriceEventLogService
    {
        Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate, EventEnum? eventEnum, LogLevelEnum? logLevelEnum, string channel, string store,
            string sku);
        Task<List<EventLog>> GetPriceEventLogWithFilter(DateTime fromDate, DateTime endDate, EventEnum? eventEnum, LogLevelEnum? logLevelEnum,
            string channel, string store, string sku, int pageIndex, int pageSize);
    }
    
    public class PriceEventLogService : IPriceEventLogService
    {
        readonly IPriceEventLogStorage priceEventLogStorage;

        public PriceEventLogService(IPriceEventLogStorage priceEventLogStorage) {
            this.priceEventLogStorage = priceEventLogStorage;
        }

        public async Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate, EventEnum? eventEnum, LogLevelEnum? logLevelEnum, string channel,
            string store, string sku)
        => await priceEventLogStorage.GetTotalRecordWithFilter(fromDate, endDate, eventEnum, logLevelEnum, channel, store, sku);
        public async Task<List<EventLog>> GetPriceEventLogWithFilter(DateTime fromDate, DateTime endDate, EventEnum? eventEnum, LogLevelEnum? logLevelEnum,
            string channel, string store, string sku, int pageIndex, int pageSize)
        => await priceEventLogStorage.GetPriceEventLogWithFilter(fromDate, endDate, eventEnum, logLevelEnum, channel, store, sku, pageIndex, pageSize);
    }
}