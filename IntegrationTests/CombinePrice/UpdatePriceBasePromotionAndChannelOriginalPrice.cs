using System;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using IntegrationTests.Domain;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests.CombinePrice
{
    public class UpdatePriceBasePromotionAndChannelOriginalPrice
    {
        readonly PriceApiHelper priceApiHelper;

        public UpdatePriceBasePromotionAndChannelOriginalPrice()
        {
            priceApiHelper = new PriceApiHelper();
        }
        
        // Mock price keys and New base promotion and channel original price.
        const string Channel = "CDS-Website";
        const string Store = "10138";
        const string NewBasePromotionPriceVat = "214.0";
        const string NewChannelOriginalPriceVat = "695.5";
        const string NewChannelOriginalPriceNonVat = "650.0";

        readonly TimeSpan priceEndTimeAfterUpdate = TimeSpan.FromSeconds(20);

        async Task SetupBasePrice(string channel, string store, string sku)
        {
            var updateBasePrice = priceApiHelper.UpdatePrice(null, store, sku,
                null, null, MockTestPrices.BasePromotionPriceVat,
                DateTime.UtcNow.AddSeconds(60),0);
            var updateChannelPrice = priceApiHelper.UpdatePrice(channel, store, sku,
                MockTestPrices.ChannelOriginalPriceVat, null, null,
                DateTime.UtcNow.AddSeconds(60));
            await Task.WhenAll(updateBasePrice, updateChannelPrice);
        }

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateAllNullPrice_PriceDontChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00001";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string expectNormalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.ChannelOriginalPriceNonVat;
            const string expectExpDetail = "Invalid payload detected, .10138:PC00001 has no prices.";

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();

            // Act
            Func<Task> act = async () =>
            {
                await priceApiHelper.UpdatePrice(null, store, sku, null,
                    null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            };
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument
                                                                       && e.Status.Detail == expectExpDetail);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }
        
        /**************/

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBaseOriginalPrice_PriceChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00002";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;

            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBaseSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00003";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            
            const string expectNormalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.ChannelOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, null, 
                salePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBasePromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00004";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string promotionPriceVat = NewBasePromotionPriceVat;
            
            const string expectNormalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.ChannelOriginalPriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, null, 
                null, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }
        
        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00005";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string channelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(channelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(channelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }
        
        /**************/

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBaseOriginalAndSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00006";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                salePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBaseOriginalAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00007";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                null, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBaseOriginalAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00008";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string channelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;


            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(channelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(channelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }
        
        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBaseSaleAndPromotionPricePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00009";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
                        
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, null, 
                salePriceVat, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBaseSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00010";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string channelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.ChannelOriginalPriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, null, 
                salePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(channelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(channelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBasePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00011";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.ChannelOriginalPriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, null, 
                null, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(channelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(channelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        /**************/

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBaseOriginalSaleAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00012";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string newSalePriceVat = MockTestPrices.BaseSalePriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                newSalePriceVat, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBaseOriginalSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00013";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string newSalePriceVat = MockTestPrices.BaseSalePriceVat;
            const string channelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                newSalePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(channelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(channelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBaseOriginalPromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00014";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                null, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(channelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(channelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBaseSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00015";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.ChannelOriginalPriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, null, 
                salePriceVat, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(channelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(channelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }
        
        /**************/

        [Fact]
        public async Task BasePromotionAndChannelOriginalPrice_UpdateBaseOriginalSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "PC00016";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                salePriceVat, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(channelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(channelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }
    }
}