using System;
using CsvHelper.Configuration.Attributes;

namespace CTO.Price.Shared.Domain
{
    public class UploadedPrice
    {
        DateTime onlineFromDate;
        DateTime onlineToDate;
        
        [Name("channel")]
        public string Channel { get; set; } = string.Empty;
        
        [Name("store")]
        public string Store { get; set; } = string.Empty;
        
        [Name("sku")]
        public string Sku { get; set; } = string.Empty;

        [Name("online_price")]
        public decimal? OnlinePrice { get; set; }
        
        [Name("online_from_date")]
        public DateTime OnlineFromDate 
        { 
            get => onlineFromDate;
            set => onlineFromDate = DateTime.SpecifyKind(value.AddHours(-7), DateTimeKind.Utc);
        }

        [Name("online_price_enabled")]
        public string OnlinePriceEnabled { get; set; } = string.Empty;

        [Name("online_to_date")]
        public DateTime OnlineToDate
        {
            get => onlineToDate;
            set => onlineToDate = DateTime.SpecifyKind(value.AddHours(-7), DateTimeKind.Utc);
        }

        [Name("jda_discount_code")]
        public string JdaDiscountCode { get; set; } = string.Empty;
    }
}