using System;

namespace CTO.Price.Shared.Extensions
{
    public static class PriceExtension
    {
        public static decimal ToVatPrice(this decimal nonVatPrice, decimal vatRate)
        {
            return Math.Round(nonVatPrice * (1 + vatRate / 100), 2);
        }
        
        public static decimal ToNonVatPrice(this decimal vatPrice, decimal vatRate)
        {
            return Math.Round(vatPrice / (1 + vatRate / 100), 2);
        }
    }
}