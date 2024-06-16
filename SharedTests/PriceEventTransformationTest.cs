using System;
using System.Collections.Generic;
using CTO.Price.Shared.Domain;
using FluentAssertions;
using RZ.Foundation;
using Xunit;
using Xunit.Abstractions;

namespace SharedTests
{
    public class PriceEventTransformationTest
    {
        private readonly ITestOutputHelper testOutputHelper;
        public PriceEventTransformationTest(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void CreatingPriceEvent_BothBaseAndChannelPriceExists_DetailsShouldBeFromChannel()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "Sku0001";

            const decimal baseVat = 8;
            const decimal channelVat = 10;
            const decimal baseSalePriceVat = 214;
            const decimal channelSalePriceVat = 107;
            var channelPriceTime = new DateTime(2020, 5, 10, 12, 50, 40, DateTimeKind.Utc);
            var channelAdditionalData = new Dictionary<string, object>()
            {
                {"1", new object()}
            };

            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                SalePrice = new PriceDescription()
                {
                    Vat = baseSalePriceVat
                },
                VatRate = baseVat
            }.ToOption();

            
            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
                SalePrice = new PriceDescription()
                {
                    Vat = channelSalePriceVat
                },
                VatRate = channelVat,
                PriceTime = channelPriceTime,
                AdditionalData = channelAdditionalData
                
            }.ToOption();
            
            //Act
            var transformed = PriceUpdatedEvent.FromModel(basePrice, channelPrice);
            
            //Assert
            transformed.Details.VatRate.Should().Be(channelVat);
            transformed.Timestamp.Should().Be(channelPriceTime);
            transformed.AdditionalData.Should().Be(channelAdditionalData);
        }
        
        [Fact]
        public void CreatingPriceEvent_OnlyBasePriceExists_DetailsShouldBeFromChannel()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "Sku0001";
            
            const decimal channelVat = 10;
            const decimal channelSalePriceVat = 107;
            var channelPriceTime = new DateTime(2020, 5, 10, 12, 50, 40, DateTimeKind.Utc);
            var channelAdditionalData = new Dictionary<string, object>()
            {
                {"1", new object()}
            };

            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
                SalePrice = new PriceDescription()
                {
                    Vat = channelSalePriceVat
                },
                VatRate = channelVat,
                PriceTime = channelPriceTime,
                AdditionalData = channelAdditionalData
                
            }.ToOption();
            
            //Act
            var transformed = PriceUpdatedEvent.FromModel(Option<PriceModel>.None(), channelPrice);
            
            //Assert
            transformed.Details.VatRate.Should().Be(channelVat);
            transformed.Timestamp.Should().Be(channelPriceTime);
            transformed.AdditionalData.Should().Be(channelAdditionalData);
        }
        
        [Fact]
        public void CreatingPriceEvent_OnlyChannelPriceExists_DetailsShouldBeFromChannel()
        {
            //Arrange
            const string store = "10138";
            const string sku = "Sku0001";

            const decimal baseVat = 8;
            const decimal baseSalePriceVat = 214;
            var basePriceTime = new DateTime(2020, 5, 10, 12, 50, 40, DateTimeKind.Utc);
            var baseAdditionalData = new Dictionary<string, object>()
            {
                {"1", new object()}
            };

            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                SalePrice = new PriceDescription()
                {
                    Vat = baseSalePriceVat
                },
                VatRate = baseVat,
                PriceTime = basePriceTime,
                AdditionalData = baseAdditionalData
            }.ToOption();

            //Act
            var transformed = PriceUpdatedEvent.FromModel(basePrice, Option<PriceModel>.None());
            
            //Assert
            transformed.Details.VatRate.Should().Be(baseVat);
            transformed.Timestamp.Should().Be(basePriceTime);
            transformed.AdditionalData.Should().Be(baseAdditionalData);
        }

        [Fact]
        public void CreatingPriceEvent_BothBaseAndChannelPriceExistsAndThereIsOnlyOnePriceEach_SpecialPriceFromChannelSpecialPriceAndNormalPriceFromBaseNormalPrice() {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "Sku0001";
            
            const decimal baseSalePriceVat = 214;
            const decimal channelSalePriceVat = 107;

            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                SalePrice = new PriceDescription()
                {
                    Vat = baseSalePriceVat
                }
            }.ToOption();
            
            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
                SalePrice = new PriceDescription()
                {
                    Vat = channelSalePriceVat
                }
            }.ToOption();

            //Act
            var transformed = PriceUpdatedEvent.FromModel(basePrice, channelPrice);
            
            //Assert
            transformed.Details.SpecialPrice!.Vat.Should().Be(channelSalePriceVat);
            transformed.Details.Price.Vat.Should().Be(baseSalePriceVat);
        }
        
        [Fact]
        public void CreatingPriceEvent_BothBaseAndChannelPriceExistsAndThereAreMultiplePricesEach_SpecialPriceFromChannelPriceSpecialPriceAndPriceEventNormalPriceFromBaseNormalPrice() {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "Sku0001";
            
            const decimal baseOriginalPriceVat = 321;
            const decimal baseSalePriceVat = 214;
            const decimal channelSalePriceVat = 107;
            const decimal channelPromotionPriceVat = 53.5m;
            
            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = baseOriginalPriceVat
                },
                SalePrice = new PriceDescription()
                {
                    Vat = baseSalePriceVat
                }
            }.ToOption();
            
            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
                SalePrice = new PriceDescription()
                {
                    Vat = channelSalePriceVat
                },
                PromotionPrice = new PriceDescription()
                {
                    Vat = channelPromotionPriceVat
                }
            }.ToOption();

            //Act
            var transformed = PriceUpdatedEvent.FromModel(basePrice, channelPrice);
            
            //Assert
            transformed.Details.SpecialPrice!.Vat.Should().Be(channelPromotionPriceVat);
            transformed.Details.Price.Vat.Should().Be(baseOriginalPriceVat); 
        }
        
        [Fact]
        public void CreatingPriceEvent_BothBaseAndChannelPriceExistsAndMultipleBasePricesAndSingleChannelPrice_SpecialPriceFromChannelNormalPriceAndNormalPriceFromBaseNormalPrice() {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "Sku0001";
            
            const decimal baseOriginalPriceVat = 321;
            const decimal baseSalePriceVat = 214;
            const decimal channelSalePriceVat = 107;

            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = baseOriginalPriceVat
                },
                SalePrice = new PriceDescription()
                {
                    Vat = baseSalePriceVat
                }
            }.ToOption();
            
            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
                SalePrice = new PriceDescription()
                {
                    Vat = channelSalePriceVat
                }
            }.ToOption();

            //Act
            var transformed = PriceUpdatedEvent.FromModel(basePrice, channelPrice);
            
            //Assert
            transformed.Details.SpecialPrice!.Vat.Should().Be(channelSalePriceVat);
            transformed.Details.Price.Vat.Should().Be(baseOriginalPriceVat); 
        }
        
        [Fact]
        public void CreatingPriceEvent_BothBaseAndChannelPriceExistsAndSingleBasePriceAndMultipleChannelPrices_SpecialPriceFromChannelPriceSpecialPriceAndPriceEventNormalPriceFromBaseNormalPrice() {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "Sku0001";
            
            const decimal baseOriginalPriceVat = 321;
            const decimal channelSalePriceVat = 107;
            const decimal channelPromotionPriceVat = 53.5m;
            
            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = baseOriginalPriceVat
                }
            }.ToOption();
            
            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
                SalePrice = new PriceDescription()
                {
                    Vat = channelSalePriceVat
                },
                PromotionPrice = new PriceDescription()
                {
                    Vat = channelPromotionPriceVat
                }
            }.ToOption();

            //Act
            var transformed = PriceUpdatedEvent.FromModel(basePrice, channelPrice);
            
            //Assert
            transformed.Details.SpecialPrice!.Vat.Should().Be(channelPromotionPriceVat);
            transformed.Details.Price.Vat.Should().Be(baseOriginalPriceVat); 
        }
        
        [Fact]
        public void CreatingPriceEvent_OnlyBasePriceExistsAndContainsMultiplePrices_SpecialPriceFromBaseSpecialPriceAndNormalPriceFromBaseNormalPrice(){
            //Arrange
            const string store = "10138";
            const string sku = "Sku0001";
            
            const decimal baseOriginalPriceVat = 321;
            const decimal baseSalePriceVat = 214;
            
            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = baseOriginalPriceVat
                },
                SalePrice = new PriceDescription()
                {
                    Vat = baseSalePriceVat
                }
            }.ToOption();
            
            //Act
            var transformed = PriceUpdatedEvent.FromModel(basePrice, Option<PriceModel>.None());
            
            //Assert
            transformed.Details.SpecialPrice!.Vat.Should().Be(baseSalePriceVat);
            transformed.Details.Price.Vat.Should().Be(baseOriginalPriceVat); 
        }
        
        /// <summary>
        /// When creating PriceEvent, if both base and channel price exists and there are multiple prices each, the created object must
        /// use the special price of channel price and use normal price of base price
        /// </summary>
        [Fact]
        public void CreatingPriceEvent_OnlyBasePriceExistsAndContainsSinglePrice_SpecialPriceNullAndNormalPriceFromBaseNormalPrice(){
            //Arrange
            const string store = "10138";
            const string sku = "Sku0001";
            
            const decimal baseOriginalPriceVat = 321;
            
            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = baseOriginalPriceVat
                }
            }.ToOption();
            
            //Act
            var transformed = PriceUpdatedEvent.FromModel(basePrice, Option<PriceModel>.None());
            
            //Assert
            transformed.Details.SpecialPrice.Should().Be(null);
            transformed.Details.Price.Vat.Should().Be(baseOriginalPriceVat); 
        }
        
        [Fact]
        public void CreatingPriceEvent_OnlyChannelPriceExistsAndContainsMultiplePrice_SpecialPriceFromChannelSpecialPriceAndNormalPriceFromChannelNormalPrice(){
            //Arrange
            const string store = "10138";
            const string sku = "Sku0001";

            const decimal channelOriginalPriceVat = 321;
            const decimal channelSalePriceVat = 214;
            
            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = channelOriginalPriceVat
                },
                SalePrice = new PriceDescription()
                {
                    Vat = channelSalePriceVat
                }
            }.ToOption();
            
            //Act
            var transformed = PriceUpdatedEvent.FromModel(Option<PriceModel>.None(), channelPrice);
            
            //Assert
            transformed.Details.SpecialPrice!.Vat.Should().Be(channelSalePriceVat);
            transformed.Details.Price.Vat.Should().Be(channelOriginalPriceVat); 
        }
        
        [Fact]
        public void CreatingPriceEvent_OnlyChannelPriceExistsAndContainsSinglePrice_SpecialPriceNullAndNormalPriceFromBaseNormalPrice(){
            //Arrange
            const string store = "10138";
            const string sku = "Sku0001";
            
            const decimal channelOriginalPriceVat = 321;
            
            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = channelOriginalPriceVat
                }
            }.ToOption();
            
            //Act
            var transformed = PriceUpdatedEvent.FromModel( Option<PriceModel>.None(), channelPrice);
            
            //Assert
            transformed.Details.SpecialPrice.Should().Be(null);
            transformed.Details.Price.Vat.Should().Be(channelOriginalPriceVat); 
        }
        
        [Fact]
        public void CreatingPriceEvent_StorePriceContainsPromoSaleOriginalPriceChannelContainsOriginalPrice_SpecialPriceFromChannelNormalPriceFromBaseNormalPrice() {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "Sku0001";
            
            const decimal baseOriginalPriceVat = 100;
            const decimal baseSalePriceVat = 80;
            const decimal basePromotionPriceVat = 70;
            const decimal channelOriginalPriceVat = 95;
            
            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = baseOriginalPriceVat
                },                
                SalePrice = new PriceDescription()
                {
                    Vat = baseSalePriceVat
                },
                PromotionPrice = new PriceDescription()
                {
                    Vat = basePromotionPriceVat
                }
            }.ToOption();
            
            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = channelOriginalPriceVat
                }
            }.ToOption();

            //Act
            var transformed = PriceUpdatedEvent.FromModel(basePrice, channelPrice);
            
            //Assert
            transformed.Details.SpecialPrice!.Vat.Should().Be(channelOriginalPriceVat);
            transformed.Details.Price.Vat.Should().Be(baseOriginalPriceVat); 
        }
        
        [Fact]
        public void CreatingPriceEvent_StorePriceContainsPromoSaleOriginalPriceChannelContainsOriginalPrice_SpecialPriceFromChannelSpecialPriceFromBaseNormalPrice() {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "Sku0001";
            
            const decimal baseOriginalPriceVat = 100;
            const decimal baseSalePriceVat = 80;
            const decimal basePromotionPriceVat = 70;
            const decimal channelOriginalPriceVat = 95;
            const decimal channelSalePriceVat = 85;
            
            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = baseOriginalPriceVat
                },                
                SalePrice = new PriceDescription()
                {
                    Vat = baseSalePriceVat
                },
                PromotionPrice = new PriceDescription()
                {
                    Vat = basePromotionPriceVat
                }
            }.ToOption();
            
            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = channelOriginalPriceVat
                },
                SalePrice = new PriceDescription()
                {
                    Vat = channelSalePriceVat
                }
            }.ToOption();

            //Act
            var transformed = PriceUpdatedEvent.FromModel(basePrice, channelPrice);
            
            //Assert
            transformed.Details.SpecialPrice!.Vat.Should().Be(channelSalePriceVat);
            transformed.Details.Price.Vat.Should().Be(baseOriginalPriceVat); 
        }
        
        [Fact]
        public void CreatingPriceEvent_SpecialPriceIsGreaterThanNormalPrice_SpecialPriceShouldBeNullNormalPriceShouldTakeOnSpecialPriceValue() {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "Sku0001";
            
            const decimal baseOriginalPriceVat = 85;
            const decimal baseSalePriceVat = 80;
            const decimal basePromotionPriceVat = 70;
            const decimal channelOriginalPriceVat = 95;
            const decimal channelSalePriceVat = 100;
            
            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = baseOriginalPriceVat
                },                
                SalePrice = new PriceDescription()
                {
                    Vat = baseSalePriceVat
                },
                PromotionPrice = new PriceDescription()
                {
                    Vat = basePromotionPriceVat
                }
            }.ToOption();
            
            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = channelOriginalPriceVat
                },
                SalePrice = new PriceDescription()
                {
                    Vat = channelSalePriceVat
                }
            }.ToOption();

            //Act
            var transformed = PriceUpdatedEvent.FromModel(basePrice, channelPrice);
            
            //Assert
            transformed.Details.SpecialPrice.Should().Be(null);
            transformed.Details.Price.Vat.Should().Be(channelSalePriceVat); 
        }
        
        [Fact]
        public void CreatingPriceEvent_NormalAndSpecialPriceIsEquivalent_SpecialPriceShouldBeNullNormalPriceShouldRetainCurrentValue() {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "Sku0001";
            
            const decimal baseOriginalPriceVat = 85;
            const decimal baseSalePriceVat = 80;
            const decimal basePromotionPriceVat = 70;
            const decimal channelOriginalPriceVat = 95;
            const decimal channelSalePriceVat = 85;
            
            var basePrice = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = baseOriginalPriceVat
                },                
                SalePrice = new PriceDescription()
                {
                    Vat = baseSalePriceVat
                },
                PromotionPrice = new PriceDescription()
                {
                    Vat = basePromotionPriceVat
                }
            }.ToOption();
            
            var channelPrice = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku),
                OriginalPrice = new PriceDescription()
                {
                    Vat = channelOriginalPriceVat
                },
                SalePrice = new PriceDescription()
                {
                    Vat = channelSalePriceVat
                }
            }.ToOption();

            //Act
            var transformed = PriceUpdatedEvent.FromModel(basePrice, channelPrice);
            
            //Assert
            transformed.Details.SpecialPrice.Should().Be(null);
            transformed.Details.Price.Vat.Should().Be(baseOriginalPriceVat); 
        }
        
    }
}