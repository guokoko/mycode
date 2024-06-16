using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Admin.Services;
using CTO.Price.Shared.Domain;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AdminTests.Service
{
    public class UserStorageTest
    {
        [Fact]
        public async Task UserStorageTest_NewDocument_ShouldCallInsertOne()
        {
            // Arrange
            const string email = "unit@test.com";
            const string userName = "unitTest";
            const string roleId = "Bson-Id";
            var id = new ObjectId();

            var applicationUser = new ApplicationUser
            {
                Id = id,
                UserName = userName,
                Email = email,
                Roles = new List<string> {roleId}
            };

            var mongoCollection = new Mock<IMongoCollection<ApplicationUser>>();
            mongoCollection.Setup(s => s.InsertOneAsync(applicationUser, 
                null, It.IsAny<CancellationToken>()));
            var userStorage = new UserStorage(mongoCollection.Object);
            
            // Act
            await userStorage.NewDocument(applicationUser);
            
            // Assert
            mongoCollection.Verify(s => s.InsertOneAsync(It.IsAny<ApplicationUser>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserStorageTest_NewDocuments_ShouldCallInsertMany()
        {
            // Arrange
            const string email = "unit@test.com";
            const string userName = "unitTest";
            const string roleId = "Bson-Id";
            var id = new ObjectId();

            var applicationUsers = new[] { new ApplicationUser
            {
                Id = id,
                UserName = userName,
                Email = email,
                Roles = new List<string> {roleId}
            }};

            var mongoCollection = new Mock<IMongoCollection<ApplicationUser>>();
            mongoCollection.Setup(s => s.InsertManyAsync(applicationUsers, 
                null, It.IsAny<CancellationToken>()));
            var userStorage = new UserStorage(mongoCollection.Object);
            
            // Act
            await userStorage.NewDocuments(applicationUsers);
            
            // Assert
            mongoCollection.Verify(s => s.InsertManyAsync(It.IsAny<IEnumerable<ApplicationUser>>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserStorageTest_UpdateDocument_ShouldReturnEditDataAndCallFindOneAndReplace()
        {
            // Arrange
            const string email = "unit@test.com";
            const string userName = "unitTest";
            const string roleId = "Bson-Id";
            var id = new ObjectId();

            var applicationUser = new ApplicationUser
            {
                Id = id,
                UserName = userName,
                Email = email,
                Roles = new List<string> {roleId}
            };

            var mongoCollection = new Mock<IMongoCollection<ApplicationUser>>();
            mongoCollection.Setup(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                applicationUser, It.IsAny<FindOneAndReplaceOptions<ApplicationUser, ApplicationUser>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(applicationUser);
            var userStorage = new UserStorage(mongoCollection.Object);
            
            // Act
            var result = await userStorage.UpdateDocument(applicationUser, s => s.Equals(applicationUser));
            
            // Assert
            result.Should().Be(applicationUser);
            mongoCollection.Verify(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                It.IsAny<ApplicationUser>(), It.IsAny<FindOneAndReplaceOptions<ApplicationUser, ApplicationUser>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserStorageTest_DeleteDocument_ShouldReturnDeleteDataAndCallFindOneAndDelete()
        {
            // Arrange
            const string email = "unit@test.com";
            const string userName = "unitTest";
            const string roleId = "Bson-Id";
            var id = new ObjectId();

            var applicationUser = new ApplicationUser
            {
                Id = id,
                UserName = userName,
                Email = email,
                Roles = new List<string> {roleId}
            };

            var mongoCollection = new Mock<IMongoCollection<ApplicationUser>>();
            mongoCollection.Setup(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<ApplicationUser>>(), 
                It.IsAny<FindOneAndDeleteOptions<ApplicationUser, ApplicationUser>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(applicationUser);
            var userStorage = new UserStorage(mongoCollection.Object);
            
            // Act
            var result = await userStorage.DeleteDocument(email, s => s.Equals(applicationUser));
            
            // Assert
            result.Should().Be(applicationUser);
            mongoCollection.Verify(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                It.IsAny<FindOneAndDeleteOptions<ApplicationUser, ApplicationUser>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserStorageTest_DeleteDocuments_ShouldReturnDeletedRowAndCallDeleteMany()
        {
            // Arrange
            const string key = "unit@test.com";
            const long deletedCount = 1;
            
            var mongoCollection = new Mock<IMongoCollection<ApplicationUser>>();
            mongoCollection.Setup(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<ApplicationUser>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(new DeleteResult.Acknowledged(deletedCount));
            var userStorage = new UserStorage(mongoCollection.Object);
            
            // Act
            var result = await userStorage.DeleteDocuments(key, s => s.Email.ToString().Equals(key));
            
            // Assert
            result.Should().Be(deletedCount);
            mongoCollection.Verify(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserStorageTest_GetUserByEmail_ShouldReturnUserRoleAndCallFind()
        {
            // Arrange
            const string email = "unit@test.com";
            const string userName = "unitTest";
            const string roleId = "Bson-Id";
            var id = new ObjectId();

            var applicationUser = new ApplicationUser
            {
                Id = id,
                UserName = userName,
                Email = email,
                Roles = new List<string> {roleId}
            };
            var applicationUsers = new List<ApplicationUser> {applicationUser};
            var applicationUserCursor = new Mock<IAsyncCursor<ApplicationUser>>();
            applicationUserCursor.Setup(s => s.Current).Returns(applicationUsers);
            applicationUserCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            applicationUserCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            var mongoCollection = new Mock<IMongoCollection<ApplicationUser>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                It.IsAny<FindOptions<ApplicationUser>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(applicationUserCursor.Object);
            var userStorage = new UserStorage(mongoCollection.Object);
            
            // Act
            var result = await userStorage.GetUserByEmail(email);
            
            // Assert
            result.IsSome.Should().Be(true);
            result.Get().Should().Be(applicationUser);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                It.IsAny<FindOptions<ApplicationUser>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserStorageTest_GetUserByName_ShouldReturnUserRoleAndCallFind()
        {
            // Arrange
            const string email = "unit@test.com";
            const string userName = "unitTest";
            const string roleId = "Bson-Id";
            var id = new ObjectId();

            var applicationUser = new ApplicationUser
            {
                Id = id,
                UserName = userName,
                Email = email,
                Roles = new List<string> {roleId}
            };
            var applicationUsers = new List<ApplicationUser> {applicationUser};
            var applicationUserCursor = new Mock<IAsyncCursor<ApplicationUser>>();
            applicationUserCursor.Setup(s => s.Current).Returns(applicationUsers);
            applicationUserCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            applicationUserCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            var mongoCollection = new Mock<IMongoCollection<ApplicationUser>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                    It.IsAny<FindOptions<ApplicationUser>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(applicationUserCursor.Object);
            var userStorage = new UserStorage(mongoCollection.Object);
            
            // Act
            var result = await userStorage.GetUserByName(userName);
            
            // Assert
            result.IsSome.Should().Be(true);
            result.Get().Should().Be(applicationUser);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                It.IsAny<FindOptions<ApplicationUser>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserStorageTest_DeleteUserWithExist_ShouldCallDeleteOneAsyncAndReturnDateDeleted()
        {
            // Arrange
            const string email = "unit@test.com";
            const string userName = "unitTest";
            const string roleId = "Bson-Id";
            const long deletedCount = 1;
            var id = new ObjectId();

            var applicationUser = new ApplicationUser
            {
                Id = id,
                UserName = userName,
                Email = email,
                Roles = new List<string> {roleId}
            };

            var mongoCollection = new Mock<IMongoCollection<ApplicationUser>>();
            mongoCollection.Setup(s => s.DeleteOneAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(new DeleteResult.Acknowledged(deletedCount));
            var userStorage = new UserStorage(mongoCollection.Object);
            
            // Act
            var apiResult = await userStorage.DeleteUser(applicationUser);
            var result = apiResult.GetSuccess();
            
            // Assert
            apiResult.IsSuccess.Should().Be(true);
            result.Should().Be(applicationUser);
            mongoCollection.Verify(v => v.DeleteOneAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserStorageTest_DeleteUserWithoutExist_ShouldCallDeleteOneAsyncAndReturnPriceServiceException()
        {
            // Arrange
            const string email = "unit@test.com";
            const string userName = "unitTest";
            const string roleId = "Bson-Id";
            const string expectExceptionMessage = "user unit@test.com delete failed.";
            const long deletedCount = 0;
            var id = new ObjectId();

            var applicationUser = new ApplicationUser
            {
                Id = id,
                UserName = userName,
                Email = email,
                Roles = new List<string> {roleId}
            };

            var mongoCollection = new Mock<IMongoCollection<ApplicationUser>>();
            mongoCollection.Setup(s => s.DeleteOneAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(new DeleteResult.Acknowledged(deletedCount));
            var userStorage = new UserStorage(mongoCollection.Object);
            
            // Act
            var apiResult = await userStorage.DeleteUser(applicationUser);
            var result = apiResult.GetFail();
            
            // Assert
            apiResult.IsFail.Should().Be(true);
            result.Message.Should().Be(expectExceptionMessage);
            mongoCollection.Verify(v => v.DeleteOneAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserStorageTest_UpdateUserWithExist_ShouldCallReplaceOneAsyncAndReturnDateUpdated()
        {
            // Arrange
            const string email = "unit@test.com";
            const string userName = "unitTest";
            const string roleId = "Bson-Id";
            const long deletedCount = 1;
            var id = new ObjectId();

            var applicationUser = new ApplicationUser
            {
                Id = id,
                UserName = userName,
                Email = email,
                Roles = new List<string> {roleId}
            };

            var mongoCollection = new Mock<IMongoCollection<ApplicationUser>>();
            mongoCollection.Setup(s => s.ReplaceOneAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                    It.IsAny<ApplicationUser>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReplaceOneResult.Acknowledged(deletedCount, deletedCount, null));
            var userStorage = new UserStorage(mongoCollection.Object);
            
            // Act
            var apiResult = await userStorage.UpdateUser(applicationUser);
            var result = apiResult.GetSuccess();
            
            // Assert
            apiResult.IsSuccess.Should().Be(true);
            result.Should().Be(applicationUser);
            mongoCollection.Verify(v => v.ReplaceOneAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                It.IsAny<ApplicationUser>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserStorageTest_UpdateUserWithoutExist_ShouldCallReplaceOneAsyncAndReturnPriceServiceException()
        {
            // Arrange
            const string email = "unit@test.com";
            const string userName = "unitTest";
            const string roleId = "Bson-Id";
            const string expectExceptionMessage = "user unit@test.com update failed.";
            const long deletedCount = 0;
            var id = new ObjectId();

            var applicationUser = new ApplicationUser
            {
                Id = id,
                UserName = userName,
                Email = email,
                Roles = new List<string> {roleId}
            };

            var mongoCollection = new Mock<IMongoCollection<ApplicationUser>>();
            mongoCollection.Setup(s => s.ReplaceOneAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                    It.IsAny<ApplicationUser>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReplaceOneResult.Acknowledged(deletedCount, deletedCount, null));
            var userStorage = new UserStorage(mongoCollection.Object);
            
            // Act
            var apiResult = await userStorage.UpdateUser(applicationUser);
            var result = apiResult.GetFail();
            
            // Assert
            apiResult.IsFail.Should().Be(true);
            result.Message.Should().Be(expectExceptionMessage);
            mongoCollection.Verify(v => v.ReplaceOneAsync(It.IsAny<FilterDefinition<ApplicationUser>>(),
                It.IsAny<ApplicationUser>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}