using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Confluent.Kafka;
using CTO.Price.Shared;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Extensions;
using CTO.Price.Shared.Services;
using Microsoft.Extensions.Options;
using RZ.Foundation;
using RZ.Foundation.Extensions;
using static CTO.Price.Shared.DocumentHelper;
using static RZ.Foundation.Prelude;
using DateTime = System.DateTime;

namespace CTO.Price.Scheduler.Actors.PriceImport
{
    public sealed class PriceHerald : ReceiveActor
    {
        static int instanceCounter;
        int id;
        
        readonly IEventLogService eventLogger;
        readonly IPerformanceCounter performanceCounter;
        readonly decimal defaultVatRate;
        readonly IPriceService priceWriter;
        readonly IScheduleService scheduleService;
        readonly ISystemLogService systemLogService;
        readonly ITopicPublisher publisher;
        readonly MessageBusOption busOption;

        public PriceHerald()
        {
            var locator = Context.System.GetExtension<ServiceLocator>();
            var dateTimeProvider = locator.GetService<IDateTimeProvider>();
            
            performanceCounter = locator.GetService<IPerformanceCounter>();
            defaultVatRate = locator.GetService<IOptionsMonitor<PriceDefaults>>().CurrentValue.VatRate;
            priceWriter = locator.GetService<IPriceService>();
            scheduleService = locator.GetService<IScheduleService>();
            
            eventLogger = locator.GetService<IEventLogService>();
            systemLogService = locator.GetService<ISystemLogService>();
            publisher = locator.GetService<ITopicPublisher>();
            busOption = locator.GetService<IOptionsMonitor<MessageBusOption>>().CurrentValue;
            
            id = Interlocked.Increment(ref instanceCounter);
            systemLogService.Info($"A herald #{id} created at {Context.Self.Path}");
            
            Receive<ActorCommand.ReplyIfReady>(_ => Sender.Tell(ActorStatus.Ready.Instance));
            ReceiveAsync<RawPrice>(price =>
            {
                performanceCounter.CountHeraldFromImporter();
                return ProcessRawPrice(price, dateTimeProvider.UtcNow()).Then(() => Sender.Tell(ActorStatus.Complete.Instance), ex => systemLogService.Info($"ProcessRawPrice execption {ex.ToJsonString()} price:{price.ToJsonString()}"));
            });
            ReceiveAsync<PriceModel>(price =>
            {
                performanceCounter.CountHeraldFromScheduler();
                return UpdatePrice(price, dateTimeProvider.UtcNow()).Then(() => Sender.Tell(ActorStatus.Complete.Instance), ex => systemLogService.Info($"UpdatePrice execption {ex.ToJsonString()} price:{price.ToJsonString()}"));
            });
        }
        
        async Task ProcessRawPrice(RawPrice price, DateTime now)
        {
            var vatRate = price.VatRate ?? defaultVatRate;

            string operation = null;
            try
            {
                if (price?.AdditionalData != null && price.AdditionalData.ContainsKey("operation") && price.AdditionalData["operation"] != null)
                {
                    operation = price.AdditionalData["operation"].ToString().Trim().ToLower();
                }
            }
            catch (Exception ex)
            {
                systemLogService.Info($"Error processing AdditionalData['operation']: {ex.Message}");
            }

            if (price.PromotionPrice == null && price.SalePrice == null && price.OriginalPrice == null && operation != "d") {
                await eventLogger.Warning(price.Channel, price.Store, price.Sku, EventEnum.NoPrices, price);
                systemLogService.Debug($"ProcessRawPrice1 price.Channel:{price.Channel}, price.Store:{price.Store}, price.Sku:{price.Sku}, EventEnum.NoPrices:{EventEnum.NoPrices}, price:{price}");
                return;
            }

            systemLogService.Debug($"ProcessRawPrice2 price.Channel:{price.Channel}, price.Store:{price.Store}, price.Sku:{price.Sku}, EventEnum.NoPrices:{EventEnum.NoPrices}, price:{price}");

            var mapped = price.ToPriceModel(vatRate, now);
            await eventLogger.Information(price.Channel ?? string.Empty, price.Store, price.Sku, EventEnum.Info,
                new { RawPrice = price, PriceModel = mapped });
            await UpdatePrice(mapped, now);

            if(RequiresScheduling(mapped, now))
            {
                await performanceCounter.CollectPerformance(CodeBlock.UpdateSchedule, async () => await scheduleService.UpdateSchedule(mapped, now));
            }

            #region Local Functions

            static bool RequiresScheduling(PriceModel priceModel, DateTime now) =>
                PriceNodesAny(priceModel, p => (p.Start.Try(d => now < d) ?? false) || (p.End.Try(d => now < d) ?? false));
            
            static bool PriceNodesAny(PriceModel priceModel, Func<PriceDescription, bool> condition) =>
                typeof(PriceModel).GetProperties().Where(p => p.PropertyType == typeof(PriceDescription))
                    .Any(p => ((PriceDescription?) p.GetValue(priceModel)).Try(condition) ?? false);

            #endregion
        }

        async Task UpdatePrice(PriceModel price, DateTime now)
        {
            var (updateResult, updatedPrice) = await performanceCounter.CollectPerformance(CodeBlock.UpdatePrice, async () => await priceWriter.UpdatePrice(price, now));
            await eventLogger.Information(price.Key.Channel, price.Key.Store, price.Key.Sku, EventEnum.Info, new { price, updateResult, updatedPrice });

            PriceModel tmpUpdatedPrice = PriceModel.EmptyPriceModel(price.Key, 0);
            updatedPrice.Map(p => tmpUpdatedPrice = p);
            if (price.Key.IsBaseKey())
            {
                systemLogService.Info($"IsBaseKey true, updateResult{updateResult}, updatedPrice {tmpUpdatedPrice.ToJsonString()}, price: {price.ToJsonString()}");
                var basePrice = updatedPrice;
                await basePrice
                    .IfNone(() => { performanceCounter.CountIgnored(); })
                    .Chain(bp => priceWriter.GetChannelKeyToUpdateFromBaseKey(bp.Key))
                    .ThenAsync(async channelKey =>
                    {
                        await (updateResult switch
                            {
                                UpdateResult.Ignored => None<PriceUpdatedEvent>(),
                                UpdateResult.Deleted => None<PriceUpdatedEvent>(),
                                _ => PriceUpdatedEvent.FromModel(basePrice, await priceWriter.GetChannelPrice(channelKey), eventLogger)
                                    .SideEffect(ue => ue.Channel = channelKey.Channel)
                            })
                            .IfNone(() => { performanceCounter.CountIgnored(); })
                            .Map(ev => ev.ToJsonString())
                            .ThenAsync(async content => { await PublishPrice(new KafkaPublishMessage(channelKey, channelKey.ToString(), content)); });
                    });
                    systemLogService.Info($"updateResult{updateResult}, updatedPrice {tmpUpdatedPrice.ToJsonString()}, price: {price.ToJsonString()}");

            }
            else 
            {
                var channelKey = price.Key;
                var channelPrice = updatedPrice;
                systemLogService.Info($"IsBaseKey true, updateResult{updateResult}, updatedPrice {tmpUpdatedPrice.ToJsonString()}, price: {price.ToJsonString()}");
                await (updateResult switch
                    {
                        UpdateResult.Ignored => None<PriceUpdatedEvent>(),
                        UpdateResult.Deleted => (await priceWriter.GetBasePrice(channelKey))
                            .Map(basePrice => PriceUpdatedEvent.FromModel(basePrice, None<PriceModel>(), eventLogger).SideEffect(ue => ue.Channel = channelKey.Channel)),
                        _ => (await priceWriter.GetBasePrice(channelKey))
                            .Map(basePrice => PriceUpdatedEvent.FromModel(basePrice, channelPrice, eventLogger).SideEffect(ue => ue.Channel = channelKey.Channel))
                    })
                    .IfNone(() => { performanceCounter.CountIgnored(); })
                    .Map(ev => ev.ToJsonString())
                    .ThenAsync(async content =>
                    {
                        await PublishPrice(new KafkaPublishMessage(channelKey, channelKey.ToString(), content));
                    });
            }
        }
        
        async Task PublishPrice(KafkaPublishMessage kafkaPublishMessage) {
            publisher.Publish(busOption.PriceAnnouncement, kafkaPublishMessage.MessageKey, kafkaPublishMessage.MessageContent, dr =>
            {
                if (dr.Error.IsError) {
                    eventLogger.Information(kafkaPublishMessage.PriceKey, EventEnum.Info, "Error PublishPrice Data: " + kafkaPublishMessage.MessageContent);
                    throw new KafkaException(dr.Error);
                }
                performanceCounter.CountOutbound();
            });
            await eventLogger.Information(kafkaPublishMessage.PriceKey, EventEnum.PublishPrice, kafkaPublishMessage.MessageContent);
        }
    }
}