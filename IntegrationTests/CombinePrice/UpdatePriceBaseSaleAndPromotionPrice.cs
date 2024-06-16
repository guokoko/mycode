using System;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using IntegrationTests.Domain;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests.CombinePrice
{
    public class UpdatePriceBaseSaleAndPromotionPrice
    {
        readonly PriceApiHelper priceApiHelper;

        public UpdatePriceBaseSaleAndPromotionPrice()
        {
            priceApiHelper = new PriceApiHelper();
        }
        
        // Mock price keys ,New base sale and promotion prices.
        const string Channel = "CDS-Website";
        const string Store = "10138";
        const string NewBaseSalePriceVat = "428.0";
        const string NewBaseSalePriceNonVat = "400.0";
        const string NewBasePromotionPriceVat = "214.0";
        const string NewBasePromotionPriceNonVat = "200.0";

        readonly TimeSpan priceEndTimeAfterUpdate = TimeSpan.FromSeconds(20);

        async Task SetupBasePrice(string store, string sku) => await priceApiHelper.UpdatePrice(null, store, sku,
            null, MockTestPrices.BaseSalePriceVat, MockTestPrices.BasePromotionPriceVat, DateTime.UtcNow.AddSeconds(60));

        [Fact]
        public async Task BaseSaleAndPromotionPrice_UpdateAllNullPrice_PriceDontChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00001";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string expectNormalPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            const string expectExpDetail = "Invalid payload detected, .10138:SP00001 has no prices.";

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);

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
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
        }
        
        /**************/

        [Fact]
        public async Task BaseSaleAndPromotionPrice_UpdateBaseOriginalPrice_PriceChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00002";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;

            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;


            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(baseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(baseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
        }

        [Fact]
        public async Task BaseSaleAndPromotionPrice_UpdateBaseSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00003";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = NewBaseSalePriceVat;
            const string salePriceNonVat = NewBaseSalePriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, null, 
                salePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(salePriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(salePriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
        }

        [Fact]
        public async Task BaseSaleAndPromotionPrice_UpdateBasePromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00004";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string promotionPriceNonVat = NewBasePromotionPriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, null, 
                null, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(promotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(promotionPriceNonVat);
        }
        
        [Fact]
        public async Task BaseSaleAndPromotionPrice_UpdateChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00005";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
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
        public async Task BaseSaleAndPromotionPrice_UpdateBaseOriginalAndSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00006";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string salePriceVat = NewBaseSalePriceVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                salePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(baseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(baseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
        }

        [Fact]
        public async Task BaseSaleAndPromotionPrice_UpdateBaseOriginalAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00007";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string promotionPriceNonVat = NewBasePromotionPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                null, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(baseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(baseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(promotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(promotionPriceNonVat);
        }

        [Fact]
        public async Task BaseSaleAndPromotionPrice_UpdateBaseOriginalAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00008";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;


            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
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
        public async Task BaseSaleAndPromotionPrice_UpdateBaseSaleAndPromotionPricePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00009";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = NewBaseSalePriceVat;
            const string salePriceNonVat = NewBaseSalePriceNonVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string promotionPriceNonVat = NewBasePromotionPriceNonVat;
                        
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, null, 
                salePriceVat, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(salePriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(salePriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(promotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(promotionPriceNonVat);
        }

        [Fact]
        public async Task BaseSaleAndPromotionPrice_UpdateBaseSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00010";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = NewBaseSalePriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
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
        public async Task BaseSaleAndPromotionPrice_UpdateBasePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00011";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
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
        public async Task BaseSaleAndPromotionPrice_UpdateBaseOriginalSaleAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00012";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string newSalePriceVat = NewBaseSalePriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string promotionPriceNonVat = NewBasePromotionPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                newSalePriceVat, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(baseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(baseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(promotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(promotionPriceNonVat);
        }

        [Fact]
        public async Task BaseSaleAndPromotionPrice_UpdateBaseOriginalSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00013";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string newSalePriceVat = NewBaseSalePriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
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
        public async Task BaseSaleAndPromotionPrice_UpdateBaseOriginalPromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00014";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
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
        public async Task BaseSaleAndPromotionPrice_UpdateBaseSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00015";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = NewBaseSalePriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            const string expectNormalPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
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
        public async Task BaseSaleAndPromotionPrice_UpdateBaseOriginalSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "SP00016";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string salePriceVat = NewBaseSalePriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
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