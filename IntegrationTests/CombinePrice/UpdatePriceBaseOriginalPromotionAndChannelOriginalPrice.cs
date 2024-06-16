using System;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using IntegrationTests.Domain;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests.CombinePrice
{
    public class UpdatePriceBaseOriginalPromotionAndChannelOriginalPrice
    {
        readonly PriceApiHelper priceApiHelper;

        public UpdatePriceBaseOriginalPromotionAndChannelOriginalPrice()
        {
            priceApiHelper = new PriceApiHelper();
        }
        
        // Mock price keys ,New base original and promotion prices.
        const string Channel = "CDS-Website";
        const string Store = "10138";
        const string NewBaseOriginalPriceVat = "749.0";
        const string NewBaseOriginalPriceNonVat = "700.0";
        const string NewBasePromotionPriceVat = "214.0";
        const string NewChannelOriginalPriceVat = "856.0";
        const string NewChannelOriginalPriceNonVat = "800.0";

        readonly TimeSpan priceEndTimeAfterUpdate = TimeSpan.FromSeconds(20);

        async Task SetupBasePrice(string channel, string store, string sku)
        {
            var updateBasePrice = priceApiHelper.UpdatePrice(null, store, sku,
                MockTestPrices.BaseOriginalPriceVat, null, MockTestPrices.BasePromotionPriceVat,
                DateTime.UtcNow.AddSeconds(60),0);
            var updateChannelPrice = priceApiHelper.UpdatePrice(channel, store, sku,
                MockTestPrices.ChannelOriginalPriceVat, null, null,
                DateTime.UtcNow.AddSeconds(60));
            await Task.WhenAll(updateBasePrice, updateChannelPrice);
        }
        
        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateAllNullPrice_PriceDontChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00001";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string expectNormalPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVat = MockTestPrices.ChannelOriginalPriceNonVat;
            const string expectExpDetail = "Invalid payload detected, .10138:OPC00001 has no prices.";

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
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBaseOriginalPrice_PriceChange()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00002";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newBaseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;

            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceNonVat;
            const string expectSpecialPriceVat = MockTestPrices.ChannelOriginalPriceVat;
            const string expectSpecialPriceNonVat = MockTestPrices.ChannelOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
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
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBaseSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00003";
            
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
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBasePromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00004";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            
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
                null, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }
        
        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00005";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string newChannelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string newChannelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;
            
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
            await priceApiHelper.UpdatePrice(channel, store, sku, newChannelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newChannelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newChannelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }
        
        /**************/

        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBaseOriginalAndSalePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00006";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newBaseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            
            const string expectNormalPriceVatBeforeChange = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.ChannelOriginalPriceNonVat;
            const string expectSpecialPriceVatAfterChange = MockTestPrices.ChannelOriginalPriceVat;
            const string expectSpecialPriceNonVatAfterChange = MockTestPrices.ChannelOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                salePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newBaseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newBaseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVatAfterChange);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVatAfterChange);
        }

        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBaseOriginalAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00007";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newBaseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            
            const string expectNormalPriceVatBeforeChange = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeChange = MockTestPrices.ChannelOriginalPriceNonVat;
            const string expectSpecialPriceVatAfterChange = MockTestPrices.ChannelOriginalPriceVat;
            const string expectSpecialPriceNonVatAfterChange = MockTestPrices.ChannelOriginalPriceNonVat;

            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeChange);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeChange);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                null, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newBaseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newBaseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVatAfterChange);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVatAfterChange);
        }

        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBaseOriginalAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00008";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newChannelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string newChannelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;


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
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, newChannelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newChannelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newChannelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }
        
        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBaseSaleAndPromotionPricePrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00009";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
                        
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
                salePriceVat, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBaseSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00010";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string newChannelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string newChannelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;

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
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, newChannelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newChannelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newChannelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBasePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00011";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string newPromotionPriceVat = MockTestPrices.BasePromotionPriceVat;
            const string newChannelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string newChannelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;

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
                null, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, newChannelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newChannelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newChannelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        /**************/

        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBaseOriginalSaleAndPromotionPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00012";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newBaseOriginalPriceNonVat = NewBaseOriginalPriceNonVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            
            const string expectNormalPriceVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectNormalPriceNonVatBeforeUpdate = MockTestPrices.ChannelOriginalPriceNonVat;
            const string expectSpecialPriceVatAfterUpdate = MockTestPrices.ChannelOriginalPriceVat;
            const string expectSpecialPriceNonVatAfterUpdate = MockTestPrices.ChannelOriginalPriceNonVat;


            // Act
            var priceInit = priceApiHelper.GetPrice(channel, store, sku);

            // Assert
            priceInit.Details.Count.Should().Be(1);
            priceInit.Details[0].Details.Price.Vat.Should().Be(expectNormalPriceVatBeforeUpdate);
            priceInit.Details[0].Details.Price.NonVat.Should().Be(expectNormalPriceNonVatBeforeUpdate);
            priceInit.Details[0].Details.SpecialPrice.Should().BeNull();
            
            // Act
            await priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                salePriceVat, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newBaseOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newBaseOriginalPriceNonVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Vat.Should().Be(expectSpecialPriceVatAfterUpdate);
            priceAfterUpdate.Details[0].Details.SpecialPrice.NonVat.Should().Be(expectSpecialPriceNonVatAfterUpdate);
        }

        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBaseOriginalSaleAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00013";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string newChannelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string newChannelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;

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
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                salePriceVat, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, newChannelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newChannelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newChannelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBaseOriginalPromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00014";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            const string newChannelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string newChannelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;

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
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                null, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, newChannelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newChannelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newChannelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }

        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBaseSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00015";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            const string newChannelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string newChannelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;

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
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, null, 
                salePriceVat, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, newChannelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newChannelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newChannelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }
        
        /**************/

        [Fact]
        public async Task BaseOriginalPromotionAndChannelOriginalPrice_UpdateBaseOriginalSalePromotionAndChannelOriginalPrice_PriceCombine()
        {
            // Arrange
            const string channel = Channel;
            const string store = Store;
            const string sku = "OPC00016";
            
            // Act
            await Task.WhenAll(SetupBasePrice(channel, store, sku));

            // Arrange
            const string newBaseOriginalPriceVat = NewBaseOriginalPriceVat;
            const string salePriceVat = MockTestPrices.BaseSalePriceVat;
            const string newPromotionPriceVat = NewBasePromotionPriceVat;
            const string newChannelOriginalPriceVat = NewChannelOriginalPriceVat;
            const string newChannelOriginalPriceNotVat = NewChannelOriginalPriceNonVat;

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
            var updateBase = priceApiHelper.UpdatePrice(null, store, sku, newBaseOriginalPriceVat, 
                salePriceVat, newPromotionPriceVat, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            var updateChannel = priceApiHelper.UpdatePrice(channel, store, sku, newChannelOriginalPriceVat, 
                null, null, DateTime.UtcNow.Add(priceEndTimeAfterUpdate));
            await Task.WhenAll(updateBase, updateChannel);
            var priceAfterUpdate = priceApiHelper.GetPrice(channel, store, sku);
            
            // Assert
            priceAfterUpdate.Details.Count.Should().Be(1);
            priceAfterUpdate.Details[0].Details.Price.Vat.Should().Be(newChannelOriginalPriceVat);
            priceAfterUpdate.Details[0].Details.Price.NonVat.Should().Be(newChannelOriginalPriceNotVat);
            priceAfterUpdate.Details[0].Details.SpecialPrice.Should().BeNull();
        }
    }
}