using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Extensions;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Moq;
using StackExchange.Redis;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using static RZ.Foundation.Prelude;

namespace SharedTests
{
    public class GetChannelPriceAndBasePricesTest
    {
        readonly TestBed<PriceService> testBed;

        public GetChannelPriceAndBasePricesTest(ITestOutputHelper testOutputHelper)
        {
            testBed = new TestBed<PriceService>(testOutputHelper);
        }
        
        [Fact]
        public async Task GetBaseAndChannelPrices_StorageContainsBasePriceAndChannelPriceForSku1_SearchResultShouldHaveBothChannelAndBasePrices()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku1 = "SKU0001";
            var skus = new[] {sku1};

            var searchKeys = skus.Select(sku => new PriceModelKey(channel, store, sku)).ToArray();
            var searchBaseKeys = searchKeys.Select(k => k.GetBaseKey());

            var storageBasePriceSku1 = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku1),
                OriginalPrice = new PriceDescription()
                {
                    Vat = 1070,
                    NonVat = 1000
                }
            };

            var storageChannelPriceSku1 = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku1),
                OriginalPrice = new PriceDescription()
                {
                    Vat = 535,
                    NonVat = 500
                }
            };

            var cacheBasePriceResult = new PriceModel[] {};
            var cacheChannelPriceResult = new PriceModel[] {};

            var storageBasePriceResult = new[] {storageBasePriceSku1};
            var storageChannelPriceResult = new[] {storageChannelPriceSku1};
            
            testBed.Fake<IPriceStorage>().Setup(s => s.GetPrices(searchBaseKeys)).ReturnsAsync(storageBasePriceResult);
            testBed.Fake<IPriceStorage>().Setup(s => s.GetPrices(searchKeys)).ReturnsAsync(storageChannelPriceResult);

            //Act
            var (prices, noPriceList) = await testBed.CreateSubject().GetBaseAndChannelPrices(searchKeys);

            //Assert
            noPriceList.Should().BeEmpty();
            prices.Should().HaveCount(1);

            var price = prices[0];
            price.BasePrice.Get().Should().Be(storageBasePriceSku1);
            price.ChannelPrice.Get().Should().Be(storageChannelPriceSku1);
        }


        [Fact]
        public async Task GetBaseAndChannelPricesWithTwoKeys_StorageContainsBasePriceForSku1AndChannelPriceForSku2_Sku1PriceHasBasePriceSku2PriceHasChannelPrice()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku1 = "SKU0001";
            const string sku2 = "SKU0002";
            var skus = new[] {sku1, sku2};

            var searchKeys = skus.Select(sku => new PriceModelKey(channel, store, sku)).ToArray();
            var searchBaseKeys = searchKeys.Select(k => k.GetBaseKey());

            var storageBasePriceSku1 = new PriceModel()
            {
                Key = new PriceModelKey(null, store, sku1),
                OriginalPrice = new PriceDescription()
                {
                    Vat = 1070,
                    NonVat = 1000
                }
            };

            var storageChannelPriceSku2 = new PriceModel()
            {
                Key = new PriceModelKey(channel, store, sku2),
                OriginalPrice = new PriceDescription()
                {
                    Vat = 535,
                    NonVat = 500
                }
            };

            var cacheBasePriceResult = new PriceModel[] {};
            var cacheChannelPriceResult = new PriceModel[] {};

            var storageBasePriceResult = new[] {storageBasePriceSku1};
            var storageChannelPriceResult = new[] {storageChannelPriceSku2};

            testBed.Fake<IPriceStorage>().Setup(s => s.GetPrices(searchBaseKeys)).ReturnsAsync(storageBasePriceResult);
            testBed.Fake<IPriceStorage>().Setup(s => s.GetPrices(searchKeys)).ReturnsAsync(storageChannelPriceResult);

            //Act
            var (prices, noPriceList) = await testBed.CreateSubject().GetBaseAndChannelPrices(searchKeys);
            
            //Assert
            noPriceList.Should().BeEmpty();
            prices.Should().HaveCount(2);

            var sku1Price = prices.Single(BaseOrChannelSkuEquivalentTo(sku1));
            var sku2Price = prices.Single(BaseOrChannelSkuEquivalentTo(sku2));

            sku1Price.BasePrice.Should().Be(Optional(storageBasePriceSku1));
            sku2Price.ChannelPrice.Should().Be(Optional(storageChannelPriceSku2));
        }
        
        [Fact]
        public async Task GetBaseAndChannelPrice_RedisNotResponding_RetrieveDataFromMongoDb()
        {
            //Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku1 = "SKU0001";
            
            
            var searchKey = new PriceModelKey(channel, store, sku1);
            var basePriceFromStorage = new PriceModel() { Key = searchKey.GetBaseKey()};
            var channelPriceFromStorage = new PriceModel() { Key = searchKey};

            testBed.Fake<IPriceStorage>().Setup(s => s.GetPrices(new[] {searchKey.GetBaseKey()})).ReturnsAsync(new[] {basePriceFromStorage});
            testBed.Fake<IPriceStorage>().Setup(s => s.GetPrices(new[] {searchKey})).ReturnsAsync(new[] {channelPriceFromStorage});

            var res = await testBed.CreateSubject().GetBaseAndChannelPrice(searchKey);

            res.IsSome.Should().BeTrue();
            res.Get().BasePrice.IsSome.Should().BeTrue();
            res.Get().ChannelPrice.IsSome.Should().BeTrue();
            
            res.Then(p => p.BasePrice.Then(b => b.Should().BeEquivalentTo(basePriceFromStorage)));
            res.Then(p => p.ChannelPrice.Then(c => c.Should().BeEquivalentTo(channelPriceFromStorage)));
        }
        
        
        private static Func<BaseAndChannelPrice, bool> BaseOrChannelSkuEquivalentTo(string sku)
        {
            return p => p.BasePrice.Map(p => p.Key.Sku == sku).GetOrDefault() || p.ChannelPrice.Map(p => p.Key.Sku == sku).GetOrDefault();
        }
    }
}