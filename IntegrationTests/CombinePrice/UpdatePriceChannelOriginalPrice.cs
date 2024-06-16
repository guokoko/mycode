using System;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using IntegrationTests.Domain;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests.CombinePrice
{
    public class UpdatePriceChannelOriginalPrice
    {
        readonly PriceApiHelper priceApiHelper;

        public UpdatePriceChannelOriginalPrice()
        {
            priceApiHelper = new PriceApiHelper();
        }
        
        // Mock price keys and New channel original price.
        const string Channel = "CDS-Website";
        const string Store = "10138";
        const string NewChannelOriginalPriceVat = "695.5";
        const string NewChannelOriginalPriceNonVat = "650.0";

        readonly TimeSpan priceEndTimeAfterUpdate = TimeSpan.FromSeconds(20);

        async Task SetupBasePrice(string channel, string store, string sku) => await priceApiHelper.UpdatePrice(channel, store, sku,
            MockTestPrices.ChannelOriginalPriceVat, null, null, DateTime.UtcNow.AddSeconds(60));

        [Fact]
        public async Task ChannelOriginalPrice_UpdateAllNullPrice_PriceDontChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00001";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string expectNormalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.ChannelOriginalPriceNonVat;
            const string expectExpDetail = "Invalid payload detected, .10138:O00001 has no prices.";

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
        public async Task ChannelOriginalPrice_UpdateBaseOriginalPrice_PriceChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00002";
            
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
        public async Task ChannelOriginalPrice_UpdateBaseSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00003";
            
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
        public async Task ChannelOriginalPrice_UpdateBasePromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00004";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            
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
        public async Task ChannelOriginalPrice_UpdateChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00005";
            
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
        public async Task ChannelOriginalPrice_UpdateBaseOriginalAndSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00006";
            
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
        public async Task ChannelOriginalPrice_UpdateBaseOriginalAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00007";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            
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
        public async Task ChannelOriginalPrice_UpdateBaseOriginalAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00008";
            
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
        public async Task ChannelOriginalPrice_UpdateBaseSaleAndPromotionPricePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00009";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
                        
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
        public async Task ChannelOriginalPrice_UpdateBaseSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00010";
            
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
        public async Task ChannelOriginalPrice_UpdateBasePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00011";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
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
        public async Task ChannelOriginalPrice_UpdateBaseOriginalSaleAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00012";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string newSalePriceVat = MockTestPrices.BaseSalePriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            
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
        public async Task ChannelOriginalPrice_UpdateBaseOriginalSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00013";
            
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
        public async Task ChannelOriginalPrice_UpdateBaseOriginalPromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00014";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
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
        public async Task ChannelOriginalPrice_UpdateBaseSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00015";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
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
        public async Task ChannelOriginalPrice_UpdateBaseOriginalSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "O00016";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string baseOriginalPriceVat = MockTestPrices.BaseOriginalPriceVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string promotionPriceVat = MockTestPrices.BasePromotionPriceVat;
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