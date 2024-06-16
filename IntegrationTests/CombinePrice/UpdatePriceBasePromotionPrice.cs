using System;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using IntegrationTests.Domain;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests.CombinePrice
{
    public class UpdatePriceBasePromotionPrice
    {
        readonly PriceApiHelper priceApiHelper;

        public UpdatePriceBasePromotionPrice()
        {
            priceApiHelper = new PriceApiHelper();
        }
        
        // Mock price keys and New base promotion price.
        const string Channel = "CDS-Website";
        const string Store = "10138";
        const string Sku = "CDS0001";
        const string NewBasePromotionPriceVat = "160.5";
        const string NewBasePromotionPriceNonVat = "150.0";

        readonly TimeSpan priceEndTimeAfterUpdate = TimeSpan.FromSeconds(20);

        async Task SetupBasePrice(string store, string sku) => await priceApiHelper.UpdatePrice(null, store, sku,
            null, null, MockTestPrices.BasePromotionPriceVat, DateTime.UtcNow.AddSeconds(60));

        [Fact]
        public async Task BasePromotionPrice_UpdateAllNullPrice_PriceDontChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00001";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string expectNormalPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            const string expectExpDetail = "Invalid payload detected, .10138:P00001 has no prices.";

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
        public async Task BasePromotionPrice_UpdateBaseOriginalPrice_PriceChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00002";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;

            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BasePromotionPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

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
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(baseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(baseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
        }

        [Fact]
        public async Task BasePromotionPrice_UpdateBaseSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00003";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string salePriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string promotionPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(promotionPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(promotionPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, null, 
                salePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(salePriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(salePriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(promotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(promotionPriceNonVat);
        }

        [Fact]
        public async Task BasePromotionPrice_UpdateBasePromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00004";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string promotionPriceNonVat = NewBasePromotionPriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            
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
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(promotionPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(promotionPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }
        
        [Fact]
        public async Task BasePromotionPrice_UpdateChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00005";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BasePromotionPriceNonVat;

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
        public async Task BasePromotionPrice_UpdateBaseOriginalAndSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00006";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BasePromotionPriceNonVat;

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
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(baseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(baseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
        }

        [Fact]
        public async Task BasePromotionPrice_UpdateBaseOriginalAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00007";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string promotionPriceNonVat = NewBasePromotionPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BasePromotionPriceNonVat;

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
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(baseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(baseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(promotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(promotionPriceNonVat);
        }

        [Fact]
        public async Task BasePromotionPrice_UpdateBaseOriginalAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00008";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;


            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BasePromotionPriceNonVat;
            
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
        public async Task BasePromotionPrice_UpdateBaseSaleAndPromotionPricePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00009";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string salePriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string promotionPriceNonVat = NewBasePromotionPriceNonVat;
                        
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BasePromotionPriceNonVat;

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
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(salePriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(salePriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(promotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(promotionPriceNonVat);
        }

        [Fact]
        public async Task BasePromotionPrice_UpdateBaseSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00010";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            
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
        public async Task BasePromotionPrice_UpdateBasePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00011";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            
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
        public async Task BasePromotionPrice_UpdateBaseOriginalSaleAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00012";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string newSalePriceVat = MockTestPrices.BaseSalePriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string promotionPriceNonVat = NewBasePromotionPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BasePromotionPriceNonVat;

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
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(baseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(baseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(promotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(promotionPriceNonVat);
        }

        [Fact]
        public async Task BasePromotionPrice_UpdateBaseOriginalSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00013";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string newSalePriceVat = MockTestPrices.BaseSalePriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BasePromotionPriceNonVat;
            
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
        public async Task BasePromotionPrice_UpdateBaseOriginalPromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00014";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BasePromotionPriceNonVat;
            
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
        public async Task BasePromotionPrice_UpdateBaseSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00015";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            
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
        public async Task BasePromotionPrice_UpdateBaseOriginalSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "P00016";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string promotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BasePromotionPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BasePromotionPriceNonVat;
            
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