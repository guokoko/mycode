using System;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using IntegrationTests.Domain;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests.CombinePrice
{
    public class UpdatePriceBaseSalePrice
    {
        readonly PriceApiHelper priceApiHelper;

        public UpdatePriceBaseSalePrice()
        {
            priceApiHelper = new PriceApiHelper();
        }
        
        // Mock price keys and New base sale price.
        const string Channel = "CDS-Website";
        const string Store = "10138";
        const string NewBaseSalePriceVat = "374.5";
        const string NewBaseSalePriceNonVat = "350.0";

        readonly TimeSpan priceEndTimeAfterUpdate = TimeSpan.FromSeconds(20);

        async Task SetupBasePrice(string store, string sku) => await priceApiHelper.UpdatePrice(null, store, sku,
            null, MockTestPrices.BaseSalePriceVat, null, DateTime.UtcNow.AddSeconds(60));
        
        [Fact]
        public async Task BaseSalePrice_UpdateAllNullPrice_PriceDontChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00001";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string expectNormalPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            const string expectExpDetail = "Invalid payload detected, .10138:S00001 has no prices.";

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
        public async Task BaseSalePrice_UpdateBaseOriginalPrice_PriceChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00002";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;

            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseSalePrice_UpdateBaseSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00003";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = NewBaseSalePriceVat;
            const string salePriceNonVat = NewBaseSalePriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, null, 
                salePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(salePriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(salePriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BaseSalePrice_UpdateBasePromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00004";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string promotionPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            
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
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(promotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(promotionPriceNonVat);
        }
        
        [Fact]
        public async Task BaseSalePrice_UpdateChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00005";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseSalePrice_UpdateBaseOriginalAndSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00006";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string baseSalePriceVat = NewBaseSalePriceVat;
            const string baseSalePriceNonVat = NewBaseSalePriceNonVat;
            
            const string expectNormalPriceVatBeforeChange = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.BaseSalePriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, baseOriginalPriceVat, 
                baseSalePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(baseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(baseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(baseSalePriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(baseSalePriceNonVat);
        }

        [Fact]
        public async Task BaseSalePrice_UpdateBaseOriginalAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00007";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string promotionPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;

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
        public async Task BaseSalePrice_UpdateBaseOriginalAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00008";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;


            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;
            
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
        public async Task BaseSalePrice_UpdateBaseSaleAndPromotionPricePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00009";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string newSalePriceVat = NewBaseSalePriceVat;
            const string newSalePriceNonVat = NewBaseSalePriceNonVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string promotionPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
                        
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseSalePriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, null, 
                newSalePriceVat, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newSalePriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newSalePriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(promotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(promotionPriceNonVat);
        }

        [Fact]
        public async Task BaseSalePrice_UpdateBaseSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00010";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = NewBaseSalePriceNonVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            
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
        public async Task BaseSalePrice_UpdateBasePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00011";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            
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
        public async Task BaseSalePrice_UpdateBaseOriginalSaleAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00012";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string baseOriginalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string newSalePriceVat = NewBaseSalePriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string promotionPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            
            const string expectNormalPriceVatBeforeChange = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.BaseSalePriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
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
        public async Task BaseSalePrice_UpdateBaseOriginalSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00013";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string newSalePriceVat = NewBaseSalePriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeChange = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.BaseSalePriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
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
        public async Task BaseSalePrice_UpdateBaseOriginalPromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00014";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeChange = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.BaseSalePriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
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
        public async Task BaseSalePrice_UpdateBaseSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00015";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string newSalePriceVat = NewBaseSalePriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVat = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseSalePriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, null, 
                newSalePriceVat, promotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
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
        public async Task BaseSalePrice_UpdateBaseOriginalSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "S00016";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string salePriceVat = NewBaseSalePriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            
            const string expectNormalPriceVatBeforeChange = MockTestPrices.BaseSalePriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.BaseSalePriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
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