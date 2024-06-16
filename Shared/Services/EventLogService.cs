using System;
using System.Threading.Tasks;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Extensions;
using CTO.Price.Shared.Log;
using Microsoft.Extensions.Logging;


namespace CTO.Price.Shared.Services
{
    public interface IEventLogService
    {
        Task Information(string channel, string store, string sku, EventEnum eventEnum, object data);
        Task Information(PriceModelKey key, EventEnum eventEnum, object data);
        Task Error(string channel, string store, string sku, EventEnum eventEnum, object data);
        Task Error(PriceModelKey key, EventEnum eventEnum, object data);
        Task Warning(string? channel, string store, string sku, EventEnum eventEnum, object data);
    }

    public class EventLogService : IEventLogService
    {
        readonly ILogger<EventLogService> logger;
        readonly string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!;
        readonly IEventLogStorage eventLogStorage;
        public EventLogService(ILogger<EventLogService> logger, IEventLogStorage eventLogStorage)
        {
            this.logger = logger;
            this.eventLogStorage = eventLogStorage;
        }

        public async Task Information(string channel, string store, string sku, EventEnum eventEnum, object data)
        {
            var eventLog = new EventLog
            {
                Identifier = new EventLogKey(channel, store, sku),
                Event = eventEnum,
                State = StateEnum.Success,
                Data = data,
                Environment = environment,
                Level = LogLevelEnum.info
            };

            logger.LogInformation(eventLog.ToJsonString());
            eventLog.Data = data is string ? data : eventLog.Data.ToJsonString();
            await eventLogStorage.NewDocument(eventLog);
        }
        
        public async Task Information(PriceModelKey key, EventEnum eventEnum, object data) =>
            await Information(key.Channel, key.Store, key.Sku, eventEnum, data);

        public async Task Warning(string? channel, string store, string sku, EventEnum eventEnum, object data)
        {
            var eventLog = new EventLog
            {
                Identifier = new EventLogKey(channel, store, sku),
                Event = eventEnum,
                State = StateEnum.Success,
                Data = data,
                Environment = environment,
                Level = LogLevelEnum.warning
            };

            logger.LogWarning(eventLog.ToJsonString());
            eventLog.Data = eventLog.Data.ToJsonString();
            await eventLogStorage.NewDocument(eventLog);
        }
        
        public async Task Error(PriceModelKey key, EventEnum eventEnum, object data)
        {
            await Error(key.Channel, key.Store, key.Sku, eventEnum, data);
        }
        
        public async Task Error(string channel, string store, string sku, EventEnum eventEnum, object data)
        {
            var eventLog = new EventLog
            {
                Identifier = new EventLogKey(channel, store, sku),
                Event = eventEnum,
                State = StateEnum.Fail,
                Data = data,
                Environment = environment,
                Level = LogLevelEnum.error
            };

            logger.LogError(eventLog.ToJsonString());
            eventLog.Data = eventLog.Data.ToJsonString();
            await eventLogStorage.NewDocument(eventLog);
        }
    }
}