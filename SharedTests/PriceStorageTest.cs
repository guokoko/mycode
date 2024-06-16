using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace SharedTests
{
    public class PriceStorageTest
    {
        [Fact]
        public async Task PriceStorageTest_NewDocument_ShouldCallInsertOne()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";

            var priceModel = new PriceModel
            {
                Key = new PriceModelKey(channel, store, sku),
                LastUpdate = new DateTime(2020, 10, 01),
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    NonVat = 100,
                    Start = new DateTime(2020, 10, 01),
                    End = new DateTime(2020, 11, 30)
                }
            };

            var mongoCollection = new Mock<IMongoCollection<PriceModel>>();
            mongoCollection.Setup(s => s.InsertOneAsync(priceModel, 
                null, It.IsAny<CancellationToken>()));
            var priceStorageTest = new PriceStorage(mongoCollection.Object);
            
            // Act
            await priceStorageTest.NewDocument(priceModel);
            
            // Assert
            mongoCollection.Verify(s => s.InsertOneAsync(It.IsAny<PriceModel>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PriceStorageTest_NewDocuments_ShouldCallInsertMany()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";

            var priceModels = new [] {new PriceModel
            {
                Key = new PriceModelKey(channel, store, sku),
                LastUpdate = new DateTime(2020, 10, 01),
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    NonVat = 100,
                    Start = new DateTime(2020, 10, 01),
                    End = new DateTime(2020, 11, 30)
                }
            }};

            var mongoCollection = new Mock<IMongoCollection<PriceModel>>();
            mongoCollection.Setup(s => s.InsertManyAsync(priceModels, 
                null, It.IsAny<CancellationToken>()));
            var priceStorageTest = new PriceStorage(mongoCollection.Object);
            
            // Act
            await priceStorageTest.NewDocuments(priceModels);
            
            // Assert
            mongoCollection.Verify(s => s.InsertManyAsync(It.IsAny<IEnumerable<PriceModel>>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PriceStorageTest_UpdateDocument_ShouldReturnEditDataAndCallFindOneAndReplace()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";

            var priceModel = new PriceModel
            {
                Key = new PriceModelKey(channel, store, sku),
                LastUpdate = new DateTime(2020, 10, 01),
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    NonVat = 100,
                    Start = new DateTime(2020, 10, 01),
                    End = new DateTime(2020, 11, 30)
                }
            };

            var mongoCollection = new Mock<IMongoCollection<PriceModel>>();
            mongoCollection.Setup(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<PriceModel>>(),
                priceModel, It.IsAny<FindOneAndReplaceOptions<PriceModel, PriceModel>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(priceModel);
            var priceStorageTest = new PriceStorage(mongoCollection.Object);
            
            // Act
            var result = await priceStorageTest.UpdateDocument(priceModel, s => s.Equals(priceModel));
            
            // Assert
            result.Should().Be(priceModel);
            mongoCollection.Verify(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<PriceModel>>(),
                It.IsAny<PriceModel>(), It.IsAny<FindOneAndReplaceOptions<PriceModel, PriceModel>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PriceStorageTest_DeleteDocument_ShouldReturnDeleteDataAndCallFindOneAndDelete()
        {
            // Arrange
            const string key = "CDS-Website:10138:CDS00001";
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";

            var priceModel = new PriceModel
            {
                Key = new PriceModelKey(channel, store, sku),
                LastUpdate = new DateTime(2020, 10, 01),
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    NonVat = 100,
                    Start = new DateTime(2020, 10, 01),
                    End = new DateTime(2020, 11, 30)
                }
            };

            var mongoCollection = new Mock<IMongoCollection<PriceModel>>();
            mongoCollection.Setup(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<PriceModel>>(), 
                It.IsAny<FindOneAndDeleteOptions<PriceModel, PriceModel>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(priceModel);
            var priceStorageTest = new PriceStorage(mongoCollection.Object);
            
            // Act
            var result = await priceStorageTest.DeleteDocument(key, s => s.Equals(priceModel));
            
            // Assert
            result.Should().Be(priceModel);
            mongoCollection.Verify(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<PriceModel>>(),
                It.IsAny<FindOneAndDeleteOptions<PriceModel, PriceModel>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PriceStorageTest_DeleteDocuments_ShouldReturnDeletedRowAndCallDeleteMany()
        {
            // Arrange
            const string key = "CDS-Website:10138:CDS00001";
            const long deletedCount = 1;
            
            var mongoCollection = new Mock<IMongoCollection<PriceModel>>();
            mongoCollection.Setup(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<PriceModel>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(new DeleteResult.Acknowledged(deletedCount));
            var priceStorageTest = new PriceStorage(mongoCollection.Object);
            
            // Act
            var result = await priceStorageTest.DeleteDocuments(key, s => s.Key.ToString().Equals(key));
            
            // Assert
            result.Should().Be(deletedCount);
            mongoCollection.Verify(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<PriceModel>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PriceStorageTest_GetPrice_ShouldReturnPriceModelAndCallFindAsync()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";
            var priceModelKey = new PriceModelKey(channel, store, sku);

            var priceModel = new PriceModel
            {
                Key = priceModelKey,
                LastUpdate = new DateTime(2020, 10, 01),
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    NonVat = 100,
                    Start = new DateTime(2020, 10, 01),
                    End = new DateTime(2020, 11, 30)
                }
            };
            var priceModels = new List<PriceModel> {priceModel};
            var priceModelCursor = new Mock<IAsyncCursor<PriceModel>>();
            priceModelCursor.Setup(s => s.Current).Returns(priceModels);
            priceModelCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            priceModelCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            var mongoCollection = new Mock<IMongoCollection<PriceModel>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<PriceModel>>(),
                It.IsAny<FindOptions<PriceModel, PriceModel>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(priceModelCursor.Object);
            var priceStore = new PriceStorage(mongoCollection.Object);
            
            // Act
            var result = await priceStore.GetPrice(priceModelKey);
            
            // Assert
            result.IsSome.Should().Be(true);
            result.Get().Should().Be(priceModel);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<PriceModel>>(),
                It.IsAny<FindOptions<PriceModel, PriceModel>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PriceStorageTest_GetPrices_ShouldReturnPriceModelAndCallFindAsync()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";
            var priceModelKey = new PriceModelKey(channel, store, sku);
            var priceModelKeys = new[] {priceModelKey};

            var priceModel = new PriceModel
            {
                Key = priceModelKey,
                LastUpdate = new DateTime(2020, 10, 01),
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    NonVat = 100,
                    Start = new DateTime(2020, 10, 01),
                    End = new DateTime(2020, 11, 30)
                }
            };
            var priceModels = new List<PriceModel> {priceModel};
            var priceModelCursor = new Mock<IAsyncCursor<PriceModel>>();
            priceModelCursor.Setup(s => s.Current).Returns(priceModels);
            priceModelCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            priceModelCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            var mongoCollection = new Mock<IMongoCollection<PriceModel>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<PriceModel>>(),
                It.IsAny<FindOptions<PriceModel, PriceModel>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(priceModelCursor.Object);
            var priceStore = new PriceStorage(mongoCollection.Object);
            
            // Act
            var result = await priceStore.GetPrices(priceModelKeys);
            
            // Assert
            result.Count().Should().Be(1);
            result[0].Should().Be(priceModel);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<PriceModel>>(),
                It.IsAny<FindOptions<PriceModel, PriceModel>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PriceStorageTest_GetPriceModelKeys_ShouldReturnPriceModelAndCallFindAsync()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";
            var priceModelKey = new PriceModelKey(channel, store, sku);

            var priceModel = new PriceModel
            {
                Key = priceModelKey,
                LastUpdate = new DateTime(2020, 10, 01),
                OriginalPrice = new PriceDescription
                {
                    Vat = 107,
                    NonVat = 100,
                    Start = new DateTime(2020, 10, 01),
                    End = new DateTime(2020, 11, 30)
                }
            };
            var priceModels = new List<PriceModel> {priceModel};
            var priceModelCursor = new Mock<IAsyncCursor<PriceModel>>();
            priceModelCursor.Setup(s => s.Current).Returns(priceModels);
            priceModelCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            priceModelCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));
            
            var mongoCollection = new Mock<IMongoCollection<PriceModel>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<PriceModel>>(),
                It.IsAny<FindOptions<PriceModel, PriceModel>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(priceModelCursor.Object);
            var priceStore = new PriceStorage(mongoCollection.Object);
            
            // Act
            var result = (await priceStore.GetPriceModelKeys(sku)).ToArray();
            
            // Assert
            result.Count().Should().Be(1);
            result[0].Should().Be(priceModelKey);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<PriceModel>>(),
                It.IsAny<FindOptions<PriceModel, PriceModel>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PriceStorageTest_TotalPriceCount_ShouldReturnRowCountAndCallEstimatedDocumentCount()
        {
            // Arrange
            const long rowCount = 100;

            var mongoCollection = new Mock<IMongoCollection<PriceModel>>();
            mongoCollection.Setup(s => s.EstimatedDocumentCountAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(rowCount);
            var priceStorage = new PriceStorage(mongoCollection.Object);

            // Act
            var result = await priceStorage.TotalPriceCount();
            
            // Assert
            result.Should().Be(rowCount);
            mongoCollection.Verify(s => s.EstimatedDocumentCountAsync(null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}