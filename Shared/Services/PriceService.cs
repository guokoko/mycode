using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Extensions;
using Microsoft.Extensions.Options;
using RZ.Foundation;
using RZ.Foundation.Extensions;
using static RZ.Foundation.Prelude;
using DateTime = System.DateTime;

// ReSharper disable AccessToModifiedClosure

namespace CTO.Price.Shared.Services
{
    public interface IPriceService
    {
        Task<Option<PriceModel>> GetChannelPrice(PriceModelKey key);
        Task<Option<PriceModel>> GetBasePrice(PriceModelKey key);
        Task<Option<BaseAndChannelPrice>> GetBaseAndChannelPrice(PriceModelKey key);
        Task<(BaseAndChannelPrice[] prices, string[] noPriceList)> GetBaseAndChannelPrices(IEnumerable<PriceModelKey> searchKeys);
        Task<(UpdateResult, Option<PriceModel>)> UpdatePrice(PriceModel priceModel, DateTime now);
        Task<IEnumerable<PriceModelKey>> GetPriceModelKeys(string sku);
        Option<PriceModelKey> GetChannelKeyToUpdateFromBaseKey(PriceModelKey baseKey);
        Task<long> TotalPriceCount();
    }
    
    public class BaseAndChannelPrice
    {
        public BaseAndChannelPrice(Option<PriceModel> basePrice, Option<PriceModel> channelPrice)
        {
            BasePrice = basePrice;
            ChannelPrice = channelPrice;
        }

        public Option<PriceModel> BasePrice { get; set; }
        public Option<PriceModel> ChannelPrice { get; set; }
    }
    
    public class PriceService : IPriceService
    {
        readonly IPriceStorage priceStorage;
        readonly PriceDefaults priceDefaults;
        readonly PublishConfiguration publishConfiguration;
        readonly IEventLogService eventLogger;
        readonly ISystemLogService systemLogService;
        readonly IDeleteSkuStorage deleteSkuStorage;


        public PriceService(IEventLogService eventLogger, IOptionsMonitor<PriceDefaults> priceDefaults, IOptionsMonitor<PublishConfiguration> publishConfiguration,
            IPriceStorage priceStorage, IDeleteSkuStorage deleteSkuStorage, ISystemLogService systemLogService)
        {
            this.priceStorage = priceStorage;
            this.priceDefaults = priceDefaults.CurrentValue;
            this.publishConfiguration = publishConfiguration.CurrentValue;
            this.eventLogger = eventLogger;
            this.systemLogService = systemLogService;
            this.deleteSkuStorage = deleteSkuStorage;
        }

        public Option<PriceModelKey> GetChannelKeyToUpdateFromBaseKey(PriceModelKey baseKey) =>
            publishConfiguration.StoreChannelMap.ContainsKey(baseKey.Store)
                ? new PriceModelKey(publishConfiguration.StoreChannelMap[baseKey.Store], baseKey.Store, baseKey.Sku)
                : None<PriceModelKey>();

        public async Task<Option<PriceModel>> GetChannelPrice(PriceModelKey key) => await priceStorage.GetPrice(key);
        public async Task<Option<PriceModel>> GetBasePrice(PriceModelKey key) => await priceStorage.GetPrice(key.GetBaseKey());
        public async Task<Option<BaseAndChannelPrice>> GetBaseAndChannelPrice(PriceModelKey key) => (await GetBaseAndChannelPrices(new List<PriceModelKey> {key})).prices.SingleOrDefault().ToOption();

        public async Task<(BaseAndChannelPrice[] prices, string[] noPriceList)> GetBaseAndChannelPrices(IEnumerable<PriceModelKey> searchKeys)
        {
            var keys = searchKeys.ToArray();
            var baseKeys = keys.Select(k => k.GetBaseKey());

            var storageBasePrices = await priceStorage.GetPrices(baseKeys);
            var storageChannelPrices = await priceStorage.GetPrices(keys);
            return PartitionPrice(keys, storageBasePrices, storageChannelPrices);
            
            static (BaseAndChannelPrice[], string[]) PartitionPrice(IEnumerable<PriceModelKey> keys, IEnumerable<PriceModel> basePrices, IEnumerable<PriceModel> channelPrices) => keys.Select(k => 
                    (k, price: new BaseAndChannelPrice(
                        basePrices.SingleOrDefault(sk => k.GetBaseKey().Equals(sk.Key)).ToOption(), 
                        channelPrices.SingleOrDefault(sk => !sk.Key.IsBaseKey() && k.Equals(sk.Key)).ToOption())))
                .Partition(i => !(i.price.BasePrice.IsNone && i.price.ChannelPrice.IsNone), 
                    i => i.price, i => i.k.Sku);
        }
        
        public async Task<(UpdateResult, Option<PriceModel>)> UpdatePrice(PriceModel priceModel, DateTime now) => await UpdatePrice(
            priceModel, 
            key => priceStorage.GetPrice(key),
            combined => DocumentHelper.TryAddNew(async () => await priceStorage.NewMongoDocumentReplace(combined)),
            (current, combined) => DocumentHelper.TryUpdateThrow(async () => await priceStorage.UpdateDocumentThrow(combined, p => p.Key == combined.Key && p.LastUpdate == current.LastUpdate)),
            current => DocumentHelper.TryDelete(async () => await priceStorage.DeleteDocument(current.Key.ToString(), p => p.Key == current.Key && p.LastUpdate == current.LastUpdate)),
            deleted => DocumentHelper.TryAddNew(async () => await deleteSkuStorage.NewDocument(deleted)),
            now
        );
        
        private async Task<(UpdateResult, Option<PriceModel>)> UpdatePrice(
            PriceModel incoming,
            Func<PriceModelKey, Task<Option<PriceModel>>> get,
            Func<PriceModel, Task<UpdateState>> create,
            Func<PriceModel, PriceModel, Task<UpdateState>> update,
            Func<PriceModel, Task<UpdateState>> delete,
            Func<DeletedSKUsModel, Task<UpdateState>> createDelete,
            DateTime now)
        {
            var formattedDateTime = now.ToString("yyyyMMddHHmmssfff");
            var key = incoming.Key;
            var current = await get(key);
            Retry:
            bool isDelete = false;
            if (incoming?.AdditionalData?.ContainsKey("operation") == true)
            {
                var operation = incoming.AdditionalData["operation"]?.ToString().Trim().ToLower();
                isDelete = operation != null && operation.Equals("d", StringComparison.OrdinalIgnoreCase);
            }

            if (incoming?.AdditionalData?.ContainsKey("online_price_enabled") == true)
            {
                var onlinePriceEnabled = incoming.AdditionalData["online_price_enabled"]?.ToString().Trim().ToLower();
                if (onlinePriceEnabled == "yes")
                {
                    incoming.OriginalPrice.UpdateTime = now;
                }
            }

            PriceModel tmpCurrent = PriceModel.EmptyPriceModel(key, priceDefaults.VatRate);
            current.Map(c => tmpCurrent = c);
            var pastUpdate = current.Map(c => !HaveNewPrice(GetPriceModelDict(c), GetPriceModelDict(incoming))).GetOrElse(() => false);
            await eventLogger.Information(incoming.Key.Channel, incoming.Key.Store, incoming.Key.Sku, EventEnum.UpdatePrice, new {tmpCurrent, incoming});
            if (pastUpdate && !isDelete)
            {
                return (UpdateResult.Ignored, None<PriceModel>());
            }

            var combined = current.GetOrElse(() => PriceModel.EmptyPriceModel(key, priceDefaults.VatRate)).CombinePrice(incoming, now);

            if (incoming.PromotionPrice == null && incoming.SalePrice != null && combined.PromotionPrice != null){
                await eventLogger.Information(incoming.Key.Channel, incoming.Key.Store, incoming.Key.Sku, EventEnum.Info, "Sale_Promo: " +combined.ToJsonString());

                PriceDescription promo_price = new PriceDescription
                {
                    UpdateTime = now,
                    Vat = incoming.SalePrice.Vat,
                    NonVat = incoming.SalePrice.NonVat,
                    Start = combined.PromotionPrice.Start,
                    End = now.AddMinutes(5)
                };
                incoming.PromotionPrice = promo_price;
                combined.PromotionPrice = promo_price;


                await eventLogger.Information(incoming.Key.Channel, incoming.Key.Store, incoming.Key.Sku, EventEnum.Info, "Sale_NoPromo: " +combined.ToJsonString());

            }

            var storageResult = !isDelete
                ? await TryAsync(() => combined.NoPriceAvailable
                    ? current.GetAsync(cur => delete(cur), () => Task.FromResult(UpdateState.Ignore))
                    : current.GetAsync(cur => update(cur, combined), () => create(combined))).Try()
                : await TryAsync(async () =>
                    {
                        //If sku is not found on PriceUpdate table then ignore it
                        if(tmpCurrent == null || tmpCurrent.NoPriceAvailable) {
                            await eventLogger.Information(incoming.Key.Channel, incoming.Key.Store, incoming.Key.Sku, EventEnum.Info, "delete sku not found in PriceUpdate: "+tmpCurrent.NoPriceAvailable);
                            return UpdateState.Ignore;
                        }

                        var deleted = new DeletedSKUsModel
                        {
                            Key = new DeletedSKUsModelKey(null, incoming.Key.Store, incoming.Key.Sku,formattedDateTime),
                            OriginalPrice = new DeletedSKUsDescription(tmpCurrent.OriginalPrice),
                            SalePrice = new DeletedSKUsDescription(tmpCurrent.SalePrice),
                            PromotionPrice = new DeletedSKUsDescription(tmpCurrent.PromotionPrice),
                            AdditionalData = incoming.AdditionalData,
                            PriceTime = incoming.PriceTime,
                            VatRate = incoming.VatRate,
                            LastUpdate = incoming.LastUpdate
                        };

                        await createDelete(deleted);
                        await current.GetAsync(cur => delete(tmpCurrent), () => Task.FromResult(UpdateState.Ignore));
                        return UpdateState.Ignore;
                    }).Try();

            switch (await storageResult.GetOrElseAsync(async e =>
                    {
                        if (e.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                        {
                            return UpdateState.Ignore;
                        }
                        var error = e switch
                        {
                        PriceServiceException ex => systemLogService.Error(ex, ex.Message),
                        _ => systemLogService.Error(e, e.Message)
                        };
                await error;
                return UpdateState.NeedRetry;
            }))
            {
                case UpdateState.NeedRetry:
                    await systemLogService.Warning($"Update price {combined.Key} failed, possible from race condition. Going to retry.");
                    now = now.AddSeconds(1);
                    goto Retry;
                    
                case UpdateState.Updated:
                    systemLogService.Info($"Updated storageResult {storageResult}, incoming.Key {incoming.Key}");
                    return (await current.Get(async _ =>
                    {
                        await eventLogger.Information(incoming.Key.Channel, incoming.Key.Store, incoming.Key.Sku, EventEnum.UpdatePrice, combined);
                        return UpdateResult.Updated;
                    }, async () =>
                    {
                        await eventLogger.Information(incoming.Key.Channel, incoming.Key.Store, incoming.Key.Sku, EventEnum.UpdatePrice, combined);
                        return UpdateResult.Created;
                    }), combined);
                
                case UpdateState.Deleted:
                    systemLogService.Info($"Deleted storageResult {storageResult}, incoming.Key {incoming.Key}");
                    await eventLogger.Information(incoming.Key.Channel, incoming.Key.Store, incoming.Key.Sku, EventEnum.UpdatePrice, combined);
                    return (UpdateResult.Deleted, combined);
                
                case UpdateState.Ignore:
                    systemLogService.Info($"Ignore storageResult {storageResult}, incoming.Key {incoming.Key}");
                    await eventLogger.Warning(incoming.Key.Channel, incoming.Key.Store, incoming.Key.Sku, EventEnum.UpdatePrice, combined);
                    return (UpdateResult.Ignored, None<PriceModel>());

                default:
                    throw new NotSupportedException($"Unrecognized UpdateResult = {storageResult}");
            }
        }

        public async Task<IEnumerable<PriceModelKey>> GetPriceModelKeys(string sku) => await priceStorage.GetPriceModelKeys(sku);

        public async Task<long> TotalPriceCount() => await priceStorage.TotalPriceCount();

        #region NeedUpdate

        static bool HaveNewPrice(ImmutableDictionary<string, DateTime?> current, ImmutableDictionary<string, DateTime?> incoming) =>
            incoming.Any(c => c.Value.Try(dt => IsNewPrice(current[c.Key], dt)) ?? false);

        static ImmutableDictionary<string, DateTime?> GetPriceModelDict(PriceModel price)
        {
            return ImmutableDictionary<string, DateTime?>.Empty
                .Add("OriginalPrice", price.OriginalPrice?.UpdateTime)
                .Add("SalePrice", price.SalePrice?.UpdateTime)
                .Add("PromotionPrice", price.PromotionPrice?.UpdateTime);
        }

        static bool IsNewPrice(DateTime? current, DateTime newUpdate) =>
            current.Try(dt =>
            {
                var timeDiff = newUpdate - dt;
                return timeDiff.TotalMilliseconds >= 0; // MongoDB's precision is in milliseconds, but .NET date/time precision is microseconds.
            }) ?? true;

        #endregion
    }
}