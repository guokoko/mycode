using System;
using CTO.Price.Shared.Domain;
using RZ.Foundation.Extensions;

namespace CTO.Price.Shared.Extensions
{
    public static class PriceModelExtension
    {
        public static PriceModel CombinePrice(this PriceModel current, PriceModel incoming, DateTime now)
        {
            var mostRecent = incoming.PriceTime < current.PriceTime ? current : incoming;
            return new PriceModel
            {
                Key = current.Key,
                VatRate = mostRecent.VatRate,
                AdditionalData = mostRecent.AdditionalData,
                PriceTime = mostRecent.PriceTime,
                LastUpdate = now,
                
                OriginalPrice = CombinePriceDescription(current.OriginalPrice, incoming.OriginalPrice, now),
                SalePrice = CombinePriceDescription(current.SalePrice, incoming.SalePrice, now),
                PromotionPrice = CombinePriceDescription(current.PromotionPrice, incoming.PromotionPrice, now)
            };
        }

        public static PriceModelKey GetBaseKey(this PriceModelKey key) => new PriceModelKey(null, key.Store, key.Sku);
        public static bool IsBaseKey(this PriceModelKey key) => key.Equals(key.GetBaseKey());

        private static PriceDescription? CombinePriceDescription(this PriceDescription? current, PriceDescription? incoming, DateTime now) {
            
            if (current == null && incoming == null)
                return null;

            PriceDescription mostRecent;
            if (current != null && incoming != null)
                mostRecent = current.UpdateTime > incoming.UpdateTime ? current : incoming;
            else
                mostRecent = (current ?? incoming)!;

            if (mostRecent.Try(p => p.Start.Try(t => t < now) ?? true) ?? false)
                if (mostRecent.Try(p => p.End.Try(t => now < t) ?? true) ?? false)
                    return mostRecent;
            
            return null;
        }
    }
}