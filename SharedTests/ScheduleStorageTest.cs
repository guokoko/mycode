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
    public class ScheduleStorageTest
    {
        [Fact]
        public async Task ScheduleStorageTest_NewDocument_ShouldCallInsertOne()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";

            var scheduleKey = new ScheduleKey(
                new DateTime(2020, 10, 01),
                new DateTime(2020, 11, 30),
                channel,
                store,
                sku
            );
            var schedule = new Schedule
            {
                Key = scheduleKey,
                LastUpdate = new DateTime(2020, 10, 01),
                PriceUpdate = new SchedulePriceUpdate
                {
                    OriginalPrice = new PriceDescription
                    {
                        Vat = 107,
                        NonVat = 100,
                        Start = new DateTime(2020, 10, 01),
                        End = new DateTime(2020, 11, 30)
                    }
                }
            };

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.InsertOneAsync(schedule, 
                null, It.IsAny<CancellationToken>()));
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);
            
            // Act
            await scheduleStorage.NewDocument(schedule);
            
            // Assert
            mongoCollection.Verify(s => s.InsertOneAsync(It.IsAny<Schedule>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);

        }
        
        [Fact]
        public async Task ScheduleStorageTest_NewDocuments_ShouldCallInsertMany()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";

            var scheduleKey = new ScheduleKey(
                new DateTime(2020, 10, 01),
                new DateTime(2020, 11, 30),
                channel,
                store,
                sku
            );
            var schedules = new[] { new Schedule
            {
                Key = scheduleKey,
                LastUpdate = new DateTime(2020, 10, 01),
                PriceUpdate = new SchedulePriceUpdate
                {
                    OriginalPrice = new PriceDescription
                    {
                        Vat = 107,
                        NonVat = 100,
                        Start = new DateTime(2020, 10, 01),
                        End = new DateTime(2020, 11, 30)
                    }
                }
            }}.ToList();

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.InsertManyAsync(schedules, 
                null, It.IsAny<CancellationToken>()));
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);
            
            // Act
            await scheduleStorage.NewDocuments(schedules);
            
            // Assert
            mongoCollection.Verify(s => s.InsertManyAsync(It.IsAny<IEnumerable<Schedule>>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleStorageTest_UpdateDocument_ShouldReturnEditDataAndCallFindOneAndReplace()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";

            var scheduleKey = new ScheduleKey(
                new DateTime(2020, 10, 01),
                new DateTime(2020, 11, 30),
                channel,
                store,
                sku
            );
            var schedule = new Schedule
            {
                Key = scheduleKey,
                LastUpdate = new DateTime(2020, 10, 01),
                PriceUpdate = new SchedulePriceUpdate
                {
                    OriginalPrice = new PriceDescription
                    {
                        Vat = 107,
                        NonVat = 100,
                        Start = new DateTime(2020, 10, 01),
                        End = new DateTime(2020, 11, 30)
                    }
                }
            };

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<Schedule>>(),
                schedule, It.IsAny<FindOneAndReplaceOptions<Schedule, Schedule>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(schedule);
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);
            
            // Act
            var result = await scheduleStorage.UpdateDocument(schedule, s => s.Equals(schedule));
            
            // Assert
            result.Should().Be(schedule);
            mongoCollection.Verify(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<Schedule>(), It.IsAny<FindOneAndReplaceOptions<Schedule, Schedule>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleStorageTest_DeleteDocument_ShouldReturnDeleteDataAndCallFindOneAndDelete()
        {
            // Arrange
            const string key = "CDS-Website:10138:CDS00001";
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";

            var scheduleKey = new ScheduleKey(
                new DateTime(2020, 10, 01),
                new DateTime(2020, 11, 30),
                channel,
                store,
                sku
            );
            var schedule = new Schedule
            {
                Key = scheduleKey,
                LastUpdate = new DateTime(2020, 10, 01),
                PriceUpdate = new SchedulePriceUpdate
                {
                    OriginalPrice = new PriceDescription
                    {
                        Vat = 107,
                        NonVat = 100,
                        Start = new DateTime(2020, 10, 01),
                        End = new DateTime(2020, 11, 30)
                    }
                }
            };

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<Schedule>>(), 
                It.IsAny<FindOneAndDeleteOptions<Schedule, Schedule>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(schedule);
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);
            
            // Act
            var result = await scheduleStorage.DeleteDocument(key, s => s.Equals(schedule));
            
            // Assert
            result.Should().Be(schedule);
            mongoCollection.Verify(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<FindOneAndDeleteOptions<Schedule, Schedule>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleStorageTest_DeleteDocuments_ShouldReturnDeletedRowAndCallDeleteMany()
        {
            // Arrange
            const string key = "CDS-Website:10138:CDS00001";
            const long deletedCount = 1;
            
            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<Schedule>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(new DeleteResult.Acknowledged(deletedCount));
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);
            
            // Act
            var result = await scheduleStorage.DeleteDocuments(key, s => s.Key.ToString().Equals(key));
            
            // Assert
            result.Should().Be(deletedCount);
            mongoCollection.Verify(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleStorageTest_UpdateSchedules_ShouldCallBulkWriteAsync()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";

            var scheduleKey = new ScheduleKey(
                new DateTime(2020, 10, 01),
                new DateTime(2020, 11, 30),
                channel,
                store,
                sku
            );
            var schedules = new[] { new Schedule
            {
                Key = scheduleKey,
                LastUpdate = new DateTime(2020, 10, 01),
                PriceUpdate = new SchedulePriceUpdate
                {
                    OriginalPrice = new PriceDescription
                    {
                        Vat = 107,
                        NonVat = 100,
                        Start = new DateTime(2020, 10, 01),
                        End = new DateTime(2020, 11, 30)
                    }
                }
            }};

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.BulkWriteAsync(It.IsAny<IEnumerable<WriteModel<Schedule>>>(),
                It.IsAny<BulkWriteOptions>(), It.IsAny<CancellationToken>()));
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);

            await scheduleStorage.UpdateSchedules(schedules);
            
            mongoCollection.Verify(s => s.BulkWriteAsync(It.IsAny<IEnumerable<WriteModel<Schedule>>>(),
                It.IsAny<BulkWriteOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task ScheduleStorageTest_UpdateScheduleWithoutDate_ShouldNotCallBulkWriteAsync()
        {
            // Arrange
            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.BulkWriteAsync(It.IsAny<IEnumerable<WriteModel<Schedule>>>(),
                It.IsAny<BulkWriteOptions>(), It.IsAny<CancellationToken>()));
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);

            await scheduleStorage.UpdateSchedules(new List<Schedule>());
            
            mongoCollection.Verify(s => s.BulkWriteAsync(It.IsAny<IEnumerable<WriteModel<Schedule>>>(),
                It.IsAny<BulkWriteOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ScheduleStorageTest_TotalScheduleCount_ShouldCallCountDocuments()
        {
            // Arrange
            const long rowCount = 100;

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.EstimatedDocumentCountAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(rowCount);
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);

            // Act
            var result = await scheduleStorage.TotalScheduleCount();
            
            // Assert
            result.Should().Be(rowCount);
            mongoCollection.Verify(s => s.EstimatedDocumentCountAsync(null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleStorageTest_TotalPendingStartSchedulesCount_ShouldCallCountDocuments()
        {
            // Arrange
            const long rowCount = 100;

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.CountDocumentsAsync(It.IsAny<FilterDefinition<Schedule>>(),
                    null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(rowCount);
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);

            // Act
            var result = await scheduleStorage.TotalPendingStartSchedulesCount();
            
            // Assert
            result.Should().Be(rowCount);
            mongoCollection.Verify(s => s.CountDocumentsAsync(It.IsAny<FilterDefinition<Schedule>>(),
                null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleStorageTest_TotalPendingEndSchedulesCount_ShouldCallCountDocuments()
        {
            // Arrange
            const long rowCount = 100;

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.CountDocumentsAsync(It.IsAny<FilterDefinition<Schedule>>(),
                    null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(rowCount);
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);

            // Act
            var result = await scheduleStorage.TotalPendingEndSchedulesCount();
            
            // Assert
            result.Should().Be(rowCount);
            mongoCollection.Verify(s => s.CountDocumentsAsync(It.IsAny<FilterDefinition<Schedule>>(),
                null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleStorageTest_GetOverlappingSchedules_ShouldReturnScheduleAndCallFind()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";

            var scheduleKey = new ScheduleKey(
                new DateTime(2020, 10, 01),
                new DateTime(2020, 11, 30),
                channel,
                store,
                sku
            );
            var schedule = new Schedule
            {
                Key = scheduleKey,
                LastUpdate = new DateTime(2020, 10, 01),
                PriceUpdate = new SchedulePriceUpdate
                {
                    OriginalPrice = new PriceDescription
                    {
                        Vat = 107,
                        NonVat = 100,
                        Start = new DateTime(2020, 10, 01),
                        End = new DateTime(2020, 11, 30)
                    }
                }
            };
            var schedules = new List<Schedule> {schedule};
            var scheduleCursor = new Mock<IAsyncCursor<Schedule>>();
            scheduleCursor.Setup(s => s.Current).Returns(schedules);
            scheduleCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            scheduleCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<FindOptions<Schedule, Schedule>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(scheduleCursor.Object);
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);
            
            // Act
            var result = await scheduleStorage.GetOverlappingSchedules(scheduleKey);
            
            // Assert
            result.Count().Should().Be(1);
            result[0].Should().Be(schedule);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<FindOptions<Schedule, Schedule>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleStorageTest_GetSchedule_ShouldReturnScheduleAndCallFind()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";

            var scheduleKey = new ScheduleKey(
                new DateTime(2020, 10, 01),
                new DateTime(2020, 11, 30),
                channel,
                store,
                sku
            );
            var schedule = new Schedule
            {
                Key = scheduleKey,
                LastUpdate = new DateTime(2020, 10, 01),
                PriceUpdate = new SchedulePriceUpdate
                {
                    OriginalPrice = new PriceDescription
                    {
                        Vat = 107,
                        NonVat = 100,
                        Start = new DateTime(2020, 10, 01),
                        End = new DateTime(2020, 11, 30)
                    }
                }
            };
            var schedules = new List<Schedule> {schedule};
            var scheduleCursor = new Mock<IAsyncCursor<Schedule>>();
            scheduleCursor.Setup(s => s.Current).Returns(schedules);
            scheduleCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            scheduleCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<FindOptions<Schedule, Schedule>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(scheduleCursor.Object);
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);
            
            // Act
            var result = await scheduleStorage.GetSchedule(scheduleKey);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Be(schedule);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<FindOptions<Schedule, Schedule>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleStorageTest_GetSchedules_ShouldReturnSchedulesAndCallFind()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";

            var scheduleKey = new ScheduleKey(
                new DateTime(2020, 10, 01),
                new DateTime(2020, 11, 30),
                channel,
                store,
                sku
            );
            var schedule = new Schedule
            {
                Key = scheduleKey,
                LastUpdate = new DateTime(2020, 10, 01),
                PriceUpdate = new SchedulePriceUpdate
                {
                    OriginalPrice = new PriceDescription
                    {
                        Vat = 107,
                        NonVat = 100,
                        Start = new DateTime(2020, 10, 01),
                        End = new DateTime(2020, 11, 30)
                    }
                }
            };
            var schedules = new List<Schedule> {schedule};
            var scheduleCursor = new Mock<IAsyncCursor<Schedule>>();
            scheduleCursor.Setup(s => s.Current).Returns(schedules);
            scheduleCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            scheduleCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<FindOptions<Schedule, Schedule>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(scheduleCursor.Object);
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);
            
            // Act
            var schedulerEnumerator = scheduleStorage.GetSchedules(channel, store, sku).GetAsyncEnumerator();
            var result = new List<Schedule>();
            while (await schedulerEnumerator.MoveNextAsync()) {
                result.Add(schedulerEnumerator.Current);
            }
            
            // Assert
            result.Count.Should().Be(1);
            result[0].Should().Be(schedule);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<FindOptions<Schedule, Schedule>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleStorageTest_GetPendingStartSchedules_ShouldReturnSchedulesAndCallFind()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";
            var cutoff = new DateTime(2020, 10, 31);

            var scheduleKey = new ScheduleKey(
                new DateTime(2020, 10, 01),
                new DateTime(2020, 11, 30),
                channel,
                store,
                sku
            );
            var schedule = new Schedule
            {
                Key = scheduleKey,
                LastUpdate = new DateTime(2020, 10, 01),
                PriceUpdate = new SchedulePriceUpdate
                {
                    OriginalPrice = new PriceDescription
                    {
                        Vat = 107,
                        NonVat = 100,
                        Start = new DateTime(2020, 10, 01),
                        End = new DateTime(2020, 11, 30)
                    }
                },
                Status = ScheduleStatus.PendingStart
            };
            var schedules = new List<Schedule> {schedule};
            var scheduleCursor = new Mock<IAsyncCursor<Schedule>>();
            scheduleCursor.Setup(s => s.Current).Returns(schedules);
            scheduleCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            scheduleCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<FindOptions<Schedule, Schedule>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(scheduleCursor.Object);
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);
            
            // Act
            var schedulerEnumerator = scheduleStorage.GetPendingStartSchedules(cutoff).GetAsyncEnumerator();
            var result = new List<Schedule>();
            while (await schedulerEnumerator.MoveNextAsync()) {
                result.Add(schedulerEnumerator.Current);
            }
            
            // Assert
            result.Count.Should().Be(1);
            result[0].Should().Be(schedule);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<FindOptions<Schedule, Schedule>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleStorageTest_GetPendingEndSchedules_ShouldReturnSchedulesAndCallFind()
        {
            // Arrange
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS00001";
            var cutoff = new DateTime(2020, 12, 01);

            var scheduleKey = new ScheduleKey(
                new DateTime(2020, 10, 01),
                new DateTime(2020, 11, 30),
                channel,
                store,
                sku
            );
            var schedule = new Schedule
            {
                Key = scheduleKey,
                LastUpdate = new DateTime(2020, 10, 01),
                PriceUpdate = new SchedulePriceUpdate
                {
                    OriginalPrice = new PriceDescription
                    {
                        Vat = 107,
                        NonVat = 100,
                        Start = new DateTime(2020, 10, 01),
                        End = new DateTime(2020, 11, 30)
                    }
                },
                Status = ScheduleStatus.PendingEnd
            };
            var schedules = new List<Schedule> {schedule};
            var scheduleCursor = new Mock<IAsyncCursor<Schedule>>();
            scheduleCursor.Setup(s => s.Current).Returns(schedules);
            scheduleCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            scheduleCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            var mongoCollection = new Mock<IMongoCollection<Schedule>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<FindOptions<Schedule, Schedule>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(scheduleCursor.Object);
            var scheduleStorage = new ScheduleStorage(mongoCollection.Object);
            
            // Act
            var schedulerEnumerator = scheduleStorage.GetPendingEndSchedules(cutoff).GetAsyncEnumerator();
            var result = new List<Schedule>();
            while (await schedulerEnumerator.MoveNextAsync()) {
                result.Add(schedulerEnumerator.Current);
            }
            
            // Assert
            result.Count.Should().Be(1);
            result[0].Should().Be(schedule);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<Schedule>>(),
                It.IsAny<FindOptions<Schedule, Schedule>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}