using System;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using IntegrationTests.Domain;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests.CombinePrice
{
    public class UpdatePriceBaseOriginalAndPromotionPrice
    {
        readonly PriceApiHelper priceApiHelper;

        public UpdatePriceBaseOriginalAndPromotionPrice()
        {
            priceApiHelper = new PriceApiHelper();
        }
        
        // Mock price keys ,New base original and promotion prices.
        const string Channel = "CDS-Website";
        const string Store = "10138";
        const string NewBaseOriginalPriceVat = "749.0";
        const string NewBaseOriginalPriceNonVat = "700.0";
        const string NewBasePromotionPriceVat = "214.0";
        const string NewBasePromotionPriceNonVat = "200.0";

        readonly TimeSpan priceEndTimeAfterUpdate = TimeSpan.FromSeconds(20);

        async Task SetupBasePrice(string store, string sku) => await priceApiHelper.UpdatePrice(null, store, sku,
            MockTestPrices.BaseOriginalPriceVat, null, MockTestPrices.BasePromotionPriceVat, DateTime.UtcNow.AddSeconds(60));

        [Fact]
        public async Task BaseOriginalAndPromotionPrice_UpdateAllNullPrice_PriceDontChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00001";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;
            const string expectExpDetail = "Invalid payload detected, .10138:OP00001 has no prices.";

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
        public async Task BaseOriginalAndPromotionPrice_UpdateBaseOriginalPrice_PriceChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00002";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newBaseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;

            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
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
            await priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newBaseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newBaseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalAndPromotionPrice_UpdateBaseSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00003";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
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
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalAndPromotionPrice_UpdateBasePromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00004";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            const string newPromotionPriceNonVat = NewBasePromotionPriceNonVat;
            
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVatBeforeUpdate = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVatBeforeUpdate = MockTestPrices.BasePromotionPriceNonVat;
            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVatBeforeUpdate);
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, null, 
                null, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(newPromotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(newPromotionPriceNonVat);
        }
        
        [Fact]
        public async Task BaseOriginalAndPromotionPrice_UpdateChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00005";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
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
        public async Task BaseOriginalAndPromotionPrice_UpdateBaseOriginalAndSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00006";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newBaseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            
            const string expectNormalPriceVatBeforeChange = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                salePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newBaseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newBaseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalAndPromotionPrice_UpdateBaseOriginalAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00007";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newBaseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            const string newPromotionPriceNonVat = NewBasePromotionPriceNonVat;
            
            const string expectNormalPriceVatBeforeChange = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                null, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newBaseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newBaseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(newPromotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(newPromotionPriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalAndPromotionPrice_UpdateBaseOriginalAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00008";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newBaseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;


            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.BaseOriginalPriceNonVat;
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
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newBaseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newBaseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(channelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(channelOriginalPriceNotVat);
        }
        
        [Fact]
        public async Task BaseOriginalAndPromotionPrice_UpdateBaseSaleAndPromotionPricePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00009";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            const string newPromotionPriceNonVat = NewBasePromotionPriceNonVat;
                        
            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
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
                salePriceVat, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(newPromotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(newPromotionPriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalAndPromotionPrice_UpdateBaseSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00010";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
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
        public async Task BaseOriginalAndPromotionPrice_UpdateBasePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00011";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string newPromotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
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
                null, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
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
        public async Task BaseOriginalAndPromotionPrice_UpdateBaseOriginalSaleAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00012";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newBaseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            const string newPromotionPriceNonVat = NewBasePromotionPriceNonVat;
            
            const string expectNormalPriceVatBeforeChange = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;


            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                salePriceVat, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newBaseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newBaseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(newPromotionPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(newPromotionPriceNonVat);
        }

        [Fact]
        public async Task BaseOriginalAndPromotionPrice_UpdateBaseOriginalSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00013";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newBaseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            const string expectNormalPriceVatBeforeChange = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            
            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                salePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newBaseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newBaseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(channelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(channelOriginalPriceNotVat);
        }

        [Fact]
        public async Task BaseOriginalAndPromotionPrice_UpdateBaseOriginalPromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00014";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newBaseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            const string expectNormalPriceVatBeforeChange = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                null, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newBaseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newBaseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(channelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(channelOriginalPriceNotVat);
        }

        [Fact]
        public async Task BaseOriginalAndPromotionPrice_UpdateBaseSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00015";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            const string expectNormalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.BaseOriginalPriceNonVat;
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
                salePriceVat, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
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
        public async Task BaseOriginalAndPromotionPrice_UpdateBaseOriginalSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OP00016";
            
            // Act
            await Task.WhenAll(SetupBasePrice(store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newBaseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            const string channelOriginalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string channelOriginalPriceNotVat = MockTestPrices.ChannelOriginalPriceNonVat;

            const string expectNormalPriceVatBeforeChange = MockTestPrices.BaseOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.BaseOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.BasePromotionPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
            priceInit.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVat);
            priceInit.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVat);
            
            // Act
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                salePriceVat, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, channelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newBaseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newBaseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(channelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(channelOriginalPriceNotVat);
        }
    }
}