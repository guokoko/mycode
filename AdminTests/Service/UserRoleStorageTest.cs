using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Admin.Services;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AdminTests.Service
{
    public class UserRoleStorageTest
    {
        [Fact]
        public async Task UserRoleStorageTest_NewDocument_ShouldCallInsertOne()
        {
            // Arrange
            const string id = "Bson-Id";
            const string role = "unit test";
            var policies = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version, RolePolicy.RegisterRole};

            var userRole = new UserRole
            {
                Id = id,
                Role = role,
                Policy = policies
            };

            var mongoCollection = new Mock<IMongoCollection<UserRole>>();
            mongoCollection.Setup(s => s.InsertOneAsync(userRole, 
                null, It.IsAny<CancellationToken>()));
            var userRoleStorage = new UserRoleStorage(mongoCollection.Object);
            
            // Act
            await userRoleStorage.NewDocument(userRole);
            
            // Assert
            mongoCollection.Verify(s => s.InsertOneAsync(It.IsAny<UserRole>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserRoleStorageTest_NewDocuments_ShouldCallInsertMany()
        {
            // Arrange
            const string id = "Bson-Id";
            const string role = "unit test";
            var policies = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version, RolePolicy.RegisterRole};

            var userRoles = new[] { new UserRole
            {
                Id = id,
                Role = role,
                Policy = policies
            }};

            var mongoCollection = new Mock<IMongoCollection<UserRole>>();
            mongoCollection.Setup(s => s.InsertManyAsync(userRoles, 
                null, It.IsAny<CancellationToken>()));
            var userRoleStorage = new UserRoleStorage(mongoCollection.Object);
            
            // Act
            await userRoleStorage.NewDocuments(userRoles);
            
            // Assert
            mongoCollection.Verify(s => s.InsertManyAsync(It.IsAny<IEnumerable<UserRole>>(), null, 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserRoleStorageTest_UpdateDocument_ShouldReturnEditDataAndCallFindOneAndReplace()
        {
            // Arrange
            const string id = "Bson-Id";
            const string role = "unit test";
            var policies = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version, RolePolicy.RegisterRole};

            var userRole = new UserRole
            {
                Id = id,
                Role = role,
                Policy = policies
            };

            var mongoCollection = new Mock<IMongoCollection<UserRole>>();
            mongoCollection.Setup(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<UserRole>>(),
                userRole, It.IsAny<FindOneAndReplaceOptions<UserRole, UserRole>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(userRole);
            var userRoleStorage = new UserRoleStorage(mongoCollection.Object);
            
            // Act
            var result = await userRoleStorage.UpdateDocument(userRole, s => s.Equals(userRole));
            
            // Assert
            result.Should().Be(userRole);
            mongoCollection.Verify(s => s.FindOneAndReplaceAsync(It.IsAny<FilterDefinition<UserRole>>(),
                It.IsAny<UserRole>(), It.IsAny<FindOneAndReplaceOptions<UserRole, UserRole>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserRoleStorageTest_DeleteDocument_ShouldReturnDeleteDataAndCallFindOneAndDelete()
        {
            // Arrange
            const string id = "Bson-Id";
            const string role = "unit test";
            var policies = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version, RolePolicy.RegisterRole};

            var userRole = new UserRole
            {
                Id = id,
                Role = role,
                Policy = policies
            };

            var mongoCollection = new Mock<IMongoCollection<UserRole>>();
            mongoCollection.Setup(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<UserRole>>(), 
                It.IsAny<FindOneAndDeleteOptions<UserRole, UserRole>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(userRole);
            var userRoleStorage = new UserRoleStorage(mongoCollection.Object);
            
            // Act
            var result = await userRoleStorage.DeleteDocument(id, s => s.Equals(userRole));
            
            // Assert
            result.Should().Be(userRole);
            mongoCollection.Verify(s => s.FindOneAndDeleteAsync(It.IsAny<FilterDefinition<UserRole>>(),
                It.IsAny<FindOneAndDeleteOptions<UserRole, UserRole>>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserRoleStorageTest_DeleteDocuments_ShouldReturnDeletedRowAndCallDeleteMany()
        {
            // Arrange
            const string key = "Bson-Id";
            const long deletedCount = 1;
            
            var mongoCollection = new Mock<IMongoCollection<UserRole>>();
            mongoCollection.Setup(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<UserRole>>(), 
                It.IsAny<CancellationToken>())).ReturnsAsync(new DeleteResult.Acknowledged(deletedCount));
            var userRoleStorage = new UserRoleStorage(mongoCollection.Object);
            
            // Act
            var result = await userRoleStorage.DeleteDocuments(key, s => s.Id.Equals(key));
            
            // Assert
            result.Should().Be(deletedCount);
            mongoCollection.Verify(s => s.DeleteManyAsync(It.IsAny<FilterDefinition<UserRole>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserRoleStorageTest_GetUserRole_ShouldReturnUserRoleAndCallFind()
        {
            // Arrange
            const string id = "Bson-Id";
            const string role = "unit test";
            var policies = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version, RolePolicy.RegisterRole};

            var userRole = new UserRole
            {
                Id = id,
                Role = role,
                Policy = policies
            };
            var userRoles = new List<UserRole> {userRole};
            var userRoleCursor = new Mock<IAsyncCursor<UserRole>>();
            userRoleCursor.Setup(s => s.Current).Returns(userRoles);
            userRoleCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            userRoleCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            var mongoCollection = new Mock<IMongoCollection<UserRole>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<UserRole>>(),
                    It.IsAny<FindOptions<UserRole>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userRoleCursor.Object);
            var userRoleStorage = new UserRoleStorage(mongoCollection.Object);
            
            // Act
            var result = await userRoleStorage.GetUserRole(role);
            
            // Assert
            result.IsSome.Should().Be(true);
            result.Get().Should().Be(userRole);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<UserRole>>(),
                It.IsAny<FindOptions<UserRole>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UserRoleStorageTest_GetUserRoleById_ShouldReturnUserRoleAndCallFind()
        {
            // Arrange
            const string id = "Bson-Id";
            const string role = "unit test";
            var policies = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version, RolePolicy.RegisterRole};

            var userRole = new UserRole
            {
                Id = id,
                Role = role,
                Policy = policies
            };
            var userRoles = new List<UserRole> {userRole};
            var userRoleCursor = new Mock<IAsyncCursor<UserRole>>();
            userRoleCursor.Setup(s => s.Current).Returns(userRoles);
            userRoleCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            userRoleCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            var mongoCollection = new Mock<IMongoCollection<UserRole>>();
            mongoCollection.Setup(s => s.FindAsync(It.IsAny<FilterDefinition<UserRole>>(),
                    It.IsAny<FindOptions<UserRole>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userRoleCursor.Object);
            var userRoleStorage = new UserRoleStorage(mongoCollection.Object);
            
            // Act
            var result = await userRoleStorage.GetUserRoleById(id);
            
            // Assert
            result.IsSome.Should().Be(true);
            result.Get().Should().Be(userRole);
            mongoCollection.Verify(s => s.FindAsync(It.IsAny<FilterDefinition<UserRole>>(),
                It.IsAny<FindOptions<UserRole>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}