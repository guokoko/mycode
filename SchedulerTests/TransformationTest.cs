using System.Collections.Generic;
using Akka.TestKit.Xunit2;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Extensions;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using DateTime = System.DateTime;

namespace SchedulerTests
{
    public class TransformationTest : TestKit
    {
        private readonly ITestOutputHelper testOutputHelper;
        public TransformationTest(ITestOutputHelper output, ITestOutputHelper testOutputHelper) : base(output: output)
        {
            this.testOutputHelper = testOutputHelper;
        }

        /// <summary>
        /// Herald should transform a raw price into a price updated event correctly.
        /// </summary>
        [Fact]
        public void SimplePriceTransform() {
            var payloadTime = new DateTime(2020,05,15, 9, 0,0);
            var processTime = new DateTime(2020,05,15, 8, 0,0);
            var vatRate = 7;
            
            var channel = "CDS-Website";
            var store = "10138";
            var sku = "123456";
            
            var payload = new RawPrice
            {
                Version = "test",
                Event = "xxxx",
                Channel = channel,
                Store = store,
                Sku = sku,
                VatRate = vatRate,
                OriginalPrice = new RawPriceDescription{ PriceVat = 107 },
                Timestamp = payloadTime
            };
            
            var expected = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
                VatRate = vatRate,
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    NonVat = 100,
                    UpdateTime = payloadTime
                },
                PriceTime = payloadTime,
                LastUpdate = processTime
            };

            var mapped = payload.ToPriceModel(vatRate, processTime);
            mapped.Should().BeEquivalentTo(expected);
        }
        
        /// <summary>
        /// Test transformation of uploaded price to raw price
        /// </summary>
        [Fact]
        public void UploadedPriceTransform() {
            
            //Arrange
            var vatRate = 7;
            var channel = "CDS-Website";
            var store = "10138";
            var sku = "123456";
            var version = "price.v1";
            var updateEvent = "price.raw";
            var priceVat = 88;
            decimal priceNonVat = new decimal(82.24);
            var onlineFromDate = new DateTime(2020,05,15, 8, 0,0);
            var onlineToDate = new DateTime(2020,06,15, 8, 0,0);
            var onlinePriceEnabled = "yes";
            var jdaDiscountCode = "(OMSF) ONLINE MKT SHARE FIX";
            var timestamp = DateTime.UtcNow;
            var hash = "hashKey";

            var payload = new UploadedPrice()
            {
                Channel = channel,
                Store = store,
                Sku = sku,
                OnlinePrice = priceVat,
                OnlineFromDate = onlineFromDate,
                OnlineToDate = onlineToDate,
                OnlinePriceEnabled = onlinePriceEnabled,
                JdaDiscountCode = jdaDiscountCode
            };
            
            var expected = new RawPrice()
            {
                Version = version,
                Channel = channel,
                Store = store,
                Sku = sku,
                VatRate = vatRate,
                Event = updateEvent,
                Timestamp = timestamp,
                OriginalPrice = new RawPriceDescription()
                {
                    PriceVat = priceVat,
                    PriceNonVat = priceNonVat, 
                    Start = onlineFromDate.AddHours(-7),
                    End = onlineToDate.AddHours(-7)
                },
                AdditionalData = new Dictionary<string, object>
                {
                    {"online_price_enabled", onlinePriceEnabled},
                    {"jda_discount_code", jdaDiscountCode}
                }
            };

            //Act
            var mapped = payload.ToRawPrice(vatRate, version, updateEvent, timestamp, hash);

            //Assert
            mapped.Should().BeEquivalentTo(expected);
        }
        
        /// <summary>
        /// Test incorrect transformation of uploaded price to raw price 
        /// </summary>
        [Fact]
        public void UploadedPriceTransformIncorrect() {
            
            //Arrange
            var vatRate = 7;
            var channel = "CDS-Website";
            var store = "10138";
            var sku = "123456";
            var version = "price.v1";
            var updateEvent = "price.raw";
            var priceVat = 88;
            decimal priceNonVat = new decimal(82.00); // incorrect non vat price
            var onlineFromDate = new DateTime(2020,05,15, 8, 0,0);
            var onlineToDate = new DateTime(2020,06,15, 8, 0,0);
            var onlinePriceEnabled = "yes";
            var jdaDiscountCode = "(OMSF) ONLINE MKT SHARE FIX";
            var timestamp = DateTime.UtcNow;

            var payload = new UploadedPrice()
            {
                Channel = channel,
                Store = store,
                Sku = sku,
                OnlinePrice = priceVat,
                OnlineFromDate = onlineFromDate,
                OnlineToDate = onlineToDate,
                OnlinePriceEnabled = onlinePriceEnabled,
                JdaDiscountCode = jdaDiscountCode
            };
            
            var expected = new RawPrice()
            {
                Version = version,
                Channel = channel,
                Store = store,
                Sku = sku,
                VatRate = vatRate,
                Event = updateEvent,
                Timestamp = timestamp,
                OriginalPrice = new RawPriceDescription()
                {
                    PriceVat = priceVat,
                    PriceNonVat = priceNonVat, 
                    Start = onlineFromDate,
                    End = onlineToDate
                },
                AdditionalData = new Dictionary<string, object>
                {
                    {"online_price_enabled", onlinePriceEnabled},
                    {"jda_discount_code", jdaDiscountCode}
                }
            };

            //Act
            var mapped = payload.ToRawPrice(vatRate, version, updateEvent, timestamp);
            
            //Assert
            mapped.Should().NotBeEquivalentTo(expected);
        }
    }
}