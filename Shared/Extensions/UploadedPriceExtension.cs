using System;
using System.Collections.Generic;
using CTO.Price.Shared.Domain;
using RZ.Foundation.Extensions;

namespace CTO.Price.Shared.Extensions
{
    public static class UploadedPriceExtension
    {
        public static RawPrice ToRawPrice(this UploadedPrice uploadedPrice, decimal vatRate, string version,  string updateEvent, DateTime now, object? hash = null)
        {
            return new RawPrice()
            {
                Version = version,
                Channel = uploadedPrice.Channel,
                Store = uploadedPrice.Store,
                Sku = uploadedPrice.Sku,
                VatRate = vatRate,
                Event = updateEvent,
                Timestamp = now,
                OriginalPrice = new RawPriceDescription()
                {
                    PriceVat = uploadedPrice.OnlinePrice,
                    PriceNonVat = uploadedPrice.OnlinePrice?.ToNonVatPrice(vatRate),
                    Start = uploadedPrice.OnlineFromDate,
                    End = uploadedPrice.OnlineToDate
                },
                AdditionalData = new Dictionary<string, object>
                {
                    {"online_price_enabled", uploadedPrice.OnlinePriceEnabled},
                    {"jda_discount_code", uploadedPrice.JdaDiscountCode}
                }
            };
        }

        public static bool ValidateOnlinePriceDigits(this UploadedPrice uploadedPrice, int digits)
        {
            var price = uploadedPrice.OnlinePrice;
            for (var i = 0; i < digits; i++) price *= 10;
            var priceText = price.ToString();

            var decimalIndex = priceText.IndexOf('.');
            if (decimalIndex == -1)
                return true;
            
            var digitsText = priceText.Substring(priceText.IndexOf('.') + 1);
            var digitsValue = int.Parse(digitsText);
            return digitsValue == 0;
        }
    }
}