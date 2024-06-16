using System;
using System.Collections.Generic;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Extensions;
using FluentAssertions;
using Moq;
using Xunit;

namespace SharedTests.Extensions
{
    public class ScheduleExtensionTest
    {
        [Fact]
        public void ScheduleExtension_CombineSchedule_ShouldReturnIncomingSchedule() {
            // Arrange
            var incomingSalePriceUpdateTime = new DateTime(2020, 6, 4).ToUniversalTime();
            var incomingOriginalPriceUpdateTime = new DateTime(2021, 6, 7).ToUniversalTime();
            var incomingPromotionPriceUpdateTime = new DateTime(2021, 6, 3).ToUniversalTime();
            
            var current = new SchedulePriceUpdate
            {
                SalePrice = null,
                OriginalPrice = null,
                PromotionPrice = null,
                AdditionalData = new Dictionary<string, object>()
                
            };
            
            var incoming = new SchedulePriceUpdate
            {
                SalePrice = new PriceDescription {UpdateTime = incomingSalePriceUpdateTime},
                OriginalPrice = new PriceDescription {UpdateTime = incomingOriginalPriceUpdateTime},
                PromotionPrice = new PriceDescription {UpdateTime = incomingPromotionPriceUpdateTime},
                AdditionalData = new Dictionary<string, object>{
                    {"testKey", "testValue"}
                }
            };
            
            var combine = new SchedulePriceUpdate
            {
                SalePrice = new PriceDescription {UpdateTime = incomingSalePriceUpdateTime},
                OriginalPrice = new PriceDescription {UpdateTime = incomingOriginalPriceUpdateTime},
                PromotionPrice = new PriceDescription {UpdateTime = incomingPromotionPriceUpdateTime},
                AdditionalData = new Dictionary<string, object>{
                    {"testKey", "testValue"}
                }
            };

            // Act
            var result = ScheduleExtension.CombineSchedule(current, incoming);

            // Assert
            result.Should().BeEquivalentTo(combine);
        }
        
        [Fact]
        public void ScheduleExtension_CombineSchedule_ShouldReturnBothNullSchedule() {
            // Arrange
            var current = new SchedulePriceUpdate
            {
                SalePrice = null,
                OriginalPrice = null,
                PromotionPrice = null,
                AdditionalData = new Dictionary<string, object>()
                
            };
            
            var incoming = new SchedulePriceUpdate
            {
                SalePrice = null,
                OriginalPrice = null,
                PromotionPrice = null,
                AdditionalData = new Dictionary<string, object>{
                    {"testKey", "testValue"}
                }
            };
            
            var combine = new SchedulePriceUpdate
            {
                SalePrice = null,
                OriginalPrice = null,
                PromotionPrice = null,
                AdditionalData = new Dictionary<string, object>{
                    {"testKey", "testValue"}
                }
            };

            // Act
            var result = ScheduleExtension.CombineSchedule(current, incoming);

            // Assert
            result.Should().BeEquivalentTo(combine);
        }
        
        [Fact]
        public void ScheduleExtension_CombineSchedule_ShouldCombineSchedule() {
            // Arrange
            var currentSalePriceUpdateTime = new DateTime(2021, 1, 5).ToUniversalTime();
            var currentOriginalPriceUpdateTime = new DateTime(2020, 1, 6).ToUniversalTime();
            var currentPromotionPriceUpdateTime = new DateTime(2021, 1, 7).ToUniversalTime();
            
            var incomingSalePriceUpdateTime = new DateTime(2020, 6, 4).ToUniversalTime();
            var incomingOriginalPriceUpdateTime = new DateTime(2021, 6, 7).ToUniversalTime();
            var incomingPromotionPriceUpdateTime = new DateTime(2021, 6, 3).ToUniversalTime();

            var current = new SchedulePriceUpdate
            {
                SalePrice = new PriceDescription {UpdateTime = currentSalePriceUpdateTime},
                OriginalPrice = new PriceDescription {UpdateTime = currentOriginalPriceUpdateTime},
                PromotionPrice = new PriceDescription {UpdateTime = currentPromotionPriceUpdateTime},
                AdditionalData = new Dictionary<string, object>()
                
            };
            
            var incoming = new SchedulePriceUpdate
            {
                SalePrice = new PriceDescription {UpdateTime = incomingSalePriceUpdateTime},
                OriginalPrice = new PriceDescription {UpdateTime = incomingOriginalPriceUpdateTime},
                PromotionPrice = new PriceDescription {UpdateTime = incomingPromotionPriceUpdateTime},
                AdditionalData = new Dictionary<string, object>{
                    {"testKey", "testValue"}
                }
            };
            
            var combine = new SchedulePriceUpdate
            {
                SalePrice = new PriceDescription {UpdateTime = currentSalePriceUpdateTime},
                OriginalPrice = new PriceDescription {UpdateTime = incomingOriginalPriceUpdateTime},
                PromotionPrice = new PriceDescription {UpdateTime = incomingPromotionPriceUpdateTime},
                AdditionalData = new Dictionary<string, object>{
                    {"testKey", "testValue"}
                }
            };
            
            // Act
            var result = ScheduleExtension.CombineSchedule(current, incoming);

            // Assert
            result.Should().BeEquivalentTo(combine);
        }
        
        [Fact]
        public void ScheduleExtension_ToPriceModel_ShouldCastToPriceModel() {
            // Arrange
            var startDate = new DateTime(2020, 05, 15).ToUniversalTime();
            var endDate = new DateTime(2020, 06, 15).ToUniversalTime();
            var salePriceUpdateTime = new DateTime(2020, 1, 5).ToUniversalTime();
            var originalPriceUpdateTime = new DateTime(2020, 1, 6).ToUniversalTime();
            var promotionPriceUpdateTime = new DateTime(2020, 1, 7).ToUniversalTime();
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "123456";
            Dictionary<string, object> additionalData = new Dictionary<string, object>();
            
            var scheduleKey = new ScheduleKey(startDate, endDate, channel, store, sku);
            var schedule = new Schedule
            {
                Key = scheduleKey,
                PriceUpdate = new SchedulePriceUpdate
                {
                    SalePrice = new PriceDescription{ UpdateTime = salePriceUpdateTime },
                    OriginalPrice = new PriceDescription{ UpdateTime =  originalPriceUpdateTime },
                    PromotionPrice = new PriceDescription{ UpdateTime =  promotionPriceUpdateTime },
                    AdditionalData = additionalData
                }
            };

            var priceModel = new PriceModel
            {
                Key = new PriceModelKey(channel, store, sku),
                OriginalPrice = new PriceDescription
                {
                    Start = startDate,
                    End = endDate,
                    UpdateTime = originalPriceUpdateTime
                },
                SalePrice = new PriceDescription
                {
                    Start = startDate,
                    End = endDate,
                    UpdateTime = salePriceUpdateTime
                },
                PromotionPrice = new PriceDescription
                {
                    Start = startDate,
                    End = endDate,
                    UpdateTime = promotionPriceUpdateTime
                },
                AdditionalData = additionalData,
                PriceTime = promotionPriceUpdateTime
            };
            
            // Act
            var result = ScheduleExtension.ToPriceModel(schedule);

            // Assert
            result.Should().BeEquivalentTo(priceModel);
        }
        
        [Fact]
        public void ScheduleExtension_CopyFrom_ShouldCopyToNewSchedule() {
            // Arrange
            var startDate = new DateTime(2020, 05, 15).ToUniversalTime();
            var endDate = new DateTime(2020, 06, 15).ToUniversalTime();
            var salePriceUpdateTime = new DateTime(2020, 1, 5).ToUniversalTime();
            var originalPriceUpdateTime = new DateTime(2020, 1, 6).ToUniversalTime();
            var promotionPriceUpdateTime = new DateTime(2020, 1, 7).ToUniversalTime();
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "123456";            
            
            var scheduleKey = new ScheduleKey(startDate, endDate, channel, store, sku);
            var schedule = new Schedule
            {
                Key = scheduleKey,
                PriceUpdate = new SchedulePriceUpdate
                {
                    SalePrice = new PriceDescription{ Vat = 1, NonVat = 2, Start = startDate, End = endDate, UpdateTime = salePriceUpdateTime },
                    OriginalPrice = new PriceDescription{ Vat = 1, NonVat = 2, Start = startDate, End = endDate, UpdateTime =  originalPriceUpdateTime },
                    PromotionPrice = new PriceDescription{ Vat = 1, NonVat = 2, Start = startDate, End = endDate, UpdateTime =  promotionPriceUpdateTime }
                },
                Status = ScheduleStatus.PendingStart,
                LastUpdate = new DateTime(2020, 1, 5).ToUniversalTime()
            };
            
            // Act
            var result = ScheduleExtension.CopyFrom(It.IsAny<Schedule>(), schedule);

            // Assert
            result.Should().BeEquivalentTo(schedule);
        }
    }
}