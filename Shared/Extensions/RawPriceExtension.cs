using System;
using CTO.Price.Shared.Domain;
using RZ.Foundation.Extensions;

namespace CTO.Price.Shared.Extensions
{
    public static class RawPriceExtension
    {
        public static PriceModel ToPriceModel(this RawPrice rawPrice, decimal vatRate, DateTime now)
        {
            return new PriceModel()
            {
                Key = new PriceModelKey(rawPrice.Channel, rawPrice.Store, rawPrice.Sku),
                AdditionalData = rawPrice.AdditionalData,
                VatRate = vatRate,
                LastUpdate = now,
                PriceTime = rawPrice.Timestamp,
                OriginalPrice = ToPriceDescription(rawPrice.OriginalPrice, vatRate),
                SalePrice = ToPriceDescription(rawPrice.SalePrice, vatRate),
                PromotionPrice = ToPriceDescription(rawPrice.PromotionPrice, vatRate)
            };

            PriceDescription? ToPriceDescription(RawPriceDescription? rawPriceDescription, decimal vatRate) =>
                rawPriceDescription.Try(rpd =>
                {
                    if (rpd.End < rpd.Start)
                        throw new ArgumentException($"Start date ({rpd.Start}) is after End date ({rpd.End}).");

                    return new PriceDescription()
                    {
                        Start = rpd.Start,
                        End = rpd.End,
                        Vat = rpd?.PriceVat ?? rpd?.PriceNonVat.Try(nv => nv * (1 + vatRate / 100)) ??
                            throw new ArgumentException("No price for both vat and non vat fields"),
                        NonVat = rpd?.PriceNonVat ?? rpd?.PriceVat.Try(v => v / (1 + vatRate / 100)) ??
                            throw new ArgumentException("No price for both vat and non vat fields"),
                        UpdateTime = rawPrice.Timestamp,
                    };
                });
        }
    }
}