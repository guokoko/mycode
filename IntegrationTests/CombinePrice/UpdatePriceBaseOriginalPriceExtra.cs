using System;
using System.Threading.Tasks;
using FluentAssertions;
using IntegrationTests.Domain;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests.CombinePrice
{
    public class UpdatePriceBaseOriginalPriceExtra
    {
        readonly PriceApiHelper priceApiHelper;

        public UpdatePriceBaseOriginalPriceExtra()
        {
            priceApiHelper = new PriceApiHelper();
        }
        
        // Base Original Price
        const string Channel = "CDS-Website";
        const string Store = "10138";

        readonly TimeSpan priceEndTimeAfterUpdate = TimeSpan.FromSeconds(20);

        async Task SetupBasePrice(string store, string sku) => await priceApiHelper.UpdatePrice(null, store, sku,
            MockTestPrices.BaseOriginalPriceVat, null, null, DateTime.UtcNow.AddSeconds(60));
        
        [Fact]
        public async Task BaseOriginalPrice_UpdateChannelSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OE00001";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string channelSalePriceVat = MockTestPrices.ChannelSalePriceVat;
            const string channelSalePriceNonVat = MockTestPrices.ChannelSalePriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(channel, store, sku, null, 
                channelSalePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(channelSalePriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(channelSalePriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalPrice_UpdateChannelPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OE00002";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string channelPromotionPriceVat = MockTestPrices.ChannelPromotionPriceVat;
            const string channelPromotionPriceNonVat = MockTestPrices.ChannelPromotionPriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(channel, store, sku, null, 
                null, channelPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(channelPromotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(channelPromotionPriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalPrice_UpdateChannelSaleAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OE00003";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string channelSalePriceVat = MockTestPrices.ChannelSalePriceVat;
            const string channelPromotionPriceVat = MockTestPrices.ChannelPromotionPriceVat;
            const string channelPromotionPriceNonVat = MockTestPrices.ChannelPromotionPriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(channel, store, sku, null, 
                channelSalePriceVat, channelPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(channelPromotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(channelPromotionPriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalPrice_UpdateChannelOriginalAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OE00004";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelPromotionPriceVat = MockTestPrices.ChannelPromotionPriceVat;
            const string channelPromotionPriceNonVat = MockTestPrices.ChannelPromotionPriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, channelPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(channelPromotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(channelPromotionPriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalPrice_UpdateChannelOriginalAndSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OE00005";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelSalePriceVat = MockTestPrices.ChannelSalePriceVat;
            const string channelSalePriceNonVat = MockTestPrices.ChannelSalePriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                channelSalePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(channelSalePriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(channelSalePriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalPrice_UpdateChannelOriginalSaleAndPromotion_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OE00006";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelSalePriceVat = MockTestPrices.ChannelSalePriceVat;
            const string channelPromotionPriceVat = MockTestPrices.ChannelPromotionPriceVat;
            const string channelPromotionPriceNonVat = MockTestPrices.ChannelPromotionPriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                channelSalePriceVat, channelPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(channelPromotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(channelPromotionPriceNonVat);
        }

    }
}