using System;
using System.Diagnostics;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RZ.Foundation;
using static RZ.Foundation.Prelude;

namespace CTO.Price.Shared.Domain
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public abstract class PriceEventBase
    {
        public string Version { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public string? Channel { get; set; } = string.Empty;
        public string Store { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public sealed class PriceValue
    {
        public decimal Vat { get; set; }
        public decimal NonVat { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public sealed class PriceDateRange
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public sealed class PriceUpdatedDetail
    {
        public decimal VatRate { get; set; }
        public PriceValue Price { get; set; } = new PriceValue();
        public PriceValue? SalePrice { get; set; } = new PriceValue();
        public PriceValue? SpecialPrice { get; set; } = new PriceValue();
        public PriceDateRange? SpecialPeriod { get; set; }
        public PriceValue? OnlinePrice { get; set; } = new PriceValue();
        public PriceDateRange? OnlinePeriod { get; set; }
        public PriceValue? PromotionPrice { get; set; } = new PriceValue();
        public PriceDateRange? PromotionPeriod { get; set; }
    }

    public sealed class PriceUpdatedEvent : PriceEventBase
    {
        public const string PriceCreated = "price.created";
        public const string PriceUpdated = "price.updated";

        public PriceUpdatedDetail Details { get; set; } = new PriceUpdatedDetail();
        public object? AdditionalData { get; set; }

        public static string GetVersionTag(int n) => $"price.updated.v{n}";

        public static PriceUpdatedEvent FromModel(Option<PriceModel> basePrice, Option<PriceModel> channelPrice, IEventLogService eventLogger = null, string @event = PriceUpdated )
        {
            Debug.Assert(basePrice.IsSome || channelPrice.IsSome);
            //TODO Will have to update how normal price is retrieved since we don't want it to default to 0 in the worst case scenario
            var primaryPrice = channelPrice.OrElse(basePrice).Get();
            var secondaryPrice = basePrice.OrElse(channelPrice).Get();

            if (eventLogger != null) {
                var logMsg = " : PriceUpdatedEvent secondaryPrice : " + JsonConvert.SerializeObject(secondaryPrice) + " :::  " + "PriceUpdatedEvent primaryPrice : " + JsonConvert.SerializeObject(primaryPrice);
                eventLogger.Information(primaryPrice.Key.Channel, primaryPrice.Key.Store, primaryPrice.Key.Sku, EventEnum.Info, logMsg);
            }
            var normalPrice = secondaryPrice.NormalPrice;
            var specialPrice = primaryPrice.SpecialPrice.OrElse(primaryPrice.NormalPrice).OrElse(secondaryPrice.SpecialPrice);

            //If special price is greater than or equal to normal price, replace normal price with special price and set special price as null. Otherwise leave as is.
            var (finalNormalPrice, finalSpecialPrice) = specialPrice.Get(
                sp => sp.Vat >= normalPrice.Vat ? (sp, None<PriceDescription>()) : (normalPrice, sp),
                () => (normalPrice, specialPrice));

            var a = new PriceUpdatedEvent
            {
                Version = GetVersionTag(1),
                Event = @event,
                Channel = primaryPrice.Key.Channel == PriceModelKey.PhysicalStoreChannel ? null : primaryPrice.Key.Channel,
                Store = primaryPrice.Key.Store,
                Sku = primaryPrice.Key.Sku,

                Details = new PriceUpdatedDetail
                {
                    VatRate = primaryPrice.VatRate,
                    Price = new PriceValue {Vat = finalNormalPrice.Vat, NonVat = finalNormalPrice.NonVat},
                    SalePrice = basePrice.Chain(ri => ri.SalePrice == null ? None<PriceValue>() : new PriceValue{Vat = ri.SalePrice.Vat, NonVat = ri.SalePrice.NonVat}.ToOption()).GetOrDefault(),
                    SpecialPrice = finalSpecialPrice.Map(p => new PriceValue {Vat = p.Vat, NonVat = p.NonVat}).GetOrDefault(),
                    SpecialPeriod = finalSpecialPrice.Map(p => new PriceDateRange {Start = p.Start, End = p.End}).GetOrDefault(),
                    OnlinePrice = channelPrice.Map(_ => finalSpecialPrice.Map(p => new PriceValue {Vat = p.Vat, NonVat = p.NonVat}).GetOrElse(new PriceValue(){Vat = finalNormalPrice.Vat, NonVat =  finalNormalPrice.NonVat})).GetOrDefault(),
                    OnlinePeriod = channelPrice.Map(_ => finalSpecialPrice.Map(p => new PriceDateRange {Start = p.Start, End = p.End}).GetOrElse(new PriceDateRange(){Start = finalNormalPrice.Start, End = finalNormalPrice.End})).GetOrDefault(),
                    PromotionPrice = secondaryPrice.SpecialPriceJDASSP.Map(p => new PriceValue { Vat = p.Vat, NonVat = p.NonVat }).GetOrDefault(),
                    PromotionPeriod = secondaryPrice.SpecialPriceJDASSP.Map(p => new PriceDateRange { Start = p.Start, End = p.End }).GetOrDefault(),
                },

                AdditionalData = primaryPrice.AdditionalData,
                Timestamp = primaryPrice.PriceTime
            };
            return a;
        }
    }

    public sealed class PriceDeletedEvent : PriceEventBase
    {
        public const string PriceDeleted = "price.deleted";

        public static string GetVersionTag(int n) => $"price.deleted.v{n}";

        public static PriceDeletedEvent FromModel(PriceModel price) =>
            new PriceDeletedEvent
            {
                Version = GetVersionTag(1),
                Event = PriceDeleted,
                Channel = price.Key.Channel,
                Store = price.Key.Store,
                Sku = price.Key.Sku,
                Timestamp = price.PriceTime
            };
    }
}