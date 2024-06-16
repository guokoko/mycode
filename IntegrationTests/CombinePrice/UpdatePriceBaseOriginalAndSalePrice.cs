using System;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using IntegrationTests.Domain;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests.CombinePrice
{
    public class UpdatePriceBaseOriginalAndSalePrice
    {
        readonly PriceApiHelper priceApiHelper;

        public UpdatePriceBaseOriginalAndSalePrice()
        {
            priceApiHelper = new PriceApiHelper();
        }
        
        // Mock price keys ,New base original and sale prices.
        const string Channel = "CDS-Website";
        const string Store = "10138";
        const string NewBaseOriginalPriceVat = "588.5";
        const string NewBaseOriginalPriceNonVat = "550.0";
        const string NewBaseSalePriceVat = "214.0";
        const string NewBaseSalePriceNonVat = "200.0";

        readonly TimeSpan priceEndTimeAfterUpdate = TimeSpan.FromSeconds(20);

        async Task SetupBasePrice(string store, string sku) => await priceApiHelper.UpdatePrice(null, store, sku,
            MockTestPrices.BaseOriginalPriceVat, MockTestPrices.BaseSalePriceVat, null, DateTime.UtcNow.AddSeconds(60));

        [Fact]
        public async Task BaseOriginalAndSalePrice_UpdateAllNullPrice_PriceDontChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00001";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            const string expectExpDetail = "Invalid payload detected, .10138:OS00001 has no prices.";

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
        public async Task BaseOriginalAndSalePrice_UpdateBaseOriginalPrice_PriceChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00002";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;

            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;


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
        public async Task BaseOriginalAndSalePrice_UpdateBaseSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00003";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = NewBaseSalePriceVat;
            const string salePriceNonVat = NewBaseSalePriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(salePriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(salePriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalAndSalePrice_UpdateBasePromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00004";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string promotionPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseOriginalAndSalePrice_UpdateChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00005";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseOriginalAndSalePrice_UpdateBaseOriginalAndSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00006";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;
            const string salePriceVat = NewBaseSalePriceVat;
            const string salePriceNonVat = NewBaseSalePriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(salePriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(salePriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalAndSalePrice_UpdateBaseOriginalAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00007";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string promotionPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseOriginalAndSalePrice_UpdateBaseOriginalAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00008";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;


            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseOriginalAndSalePrice_UpdateBaseSaleAndPromotionPricePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00009";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = NewBaseSalePriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string promotionPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
                        
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(promotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(promotionPriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalAndSalePrice_UpdateBaseSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00010";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseOriginalAndSalePrice_UpdateBasePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00011";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseOriginalAndSalePrice_UpdateBaseOriginalSaleAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00012";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string newSalePriceVat = MockTestPrices.BaseSalePriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string promotionPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseOriginalAndSalePrice_UpdateBaseOriginalSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00013";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newSalePriceVat = NewBaseSalePriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseOriginalAndSalePrice_UpdateBaseOriginalPromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00014";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseOriginalAndSalePrice_UpdateBaseSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00015";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = NewBaseSalePriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseOriginalAndSalePrice_UpdateBaseOriginalSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OS00016";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string salePriceVat = NewBaseSalePriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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