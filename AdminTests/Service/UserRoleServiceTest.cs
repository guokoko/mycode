using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Admin.Services;
using CTO.Price.Shared;
using FluentAssertions;
using Moq;
using RZ.Foundation;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;
using static RZ.Foundation.Prelude;


namespace AdminTests.Service
{
    public class UserRoleServiceTest
    {
        readonly TestBed<UserRoleService> testBed;

        public UserRoleServiceTest(ITestOutputHelper output)
        {
            testBed = new TestBed<UserRoleService>(output);
        }

        [Fact]
        public async Task UserRoleServiceTest_GetAllUserRole_ShouldReturnUserRoleList()
        {
            // Arrange
            const string roleId = "Bson-Id";
            const string roleDescription = "test_role";
            var userRole = new UserRole
            {
                Id = roleId,
                Role = roleDescription,
                Policy = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version}
            };
            var userRoles = new List<UserRole> {userRole};

            testBed.Fake<IUserRoleStorage>()
                .Setup(s => s.GetAllUserRole())
                .ReturnsAsync(userRoles);
            var userRoleService = testBed.CreateSubject();
            
            // Act
            var result = await userRoleService.GetAllUserRole();
            
            // Assert
            result.Count.Should().Be(userRoles.Count);
            result[0].Should().Be(userRole);
        }

        [Fact]
        public async Task UserRoleServiceTest_GetUserRole_ShouldReturnUserRole()
        {
            // Arrange
            const string roleId = "Bson-Id";
            const string roleDescription = "test_role";
            var userRole = new UserRole
            {
                Id = roleId,
                Role = roleDescription,
                Policy = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version}
            };
            var userRoleOpt = Optional(userRole);

            testBed.Fake<IUserRoleStorage>()
                .Setup(s => s.GetUserRole(It.IsAny<string>()))
                .ReturnsAsync(userRoleOpt);
            var userRoleService = testBed.CreateSubject();
            
            // Act
            var result = await userRoleService.GetUserRole(roleDescription);
            
            // Assert
            result.Should().Be(userRoleOpt);
            result.IsSome.Should().Be(true);
            result.Get().Should().Be(userRole);
        }

        [Fact]
        public async Task UserRoleServiceTest_GetUserRoleById_ShouldReturnUserRole()
        {
            // Arrange
            const string roleId = "Bson-Id";
            const string roleDescription = "test_role";
            var userRole = new UserRole
            {
                Id = roleId,
                Role = roleDescription,
                Policy = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version}
            };
            var userRoleOpt = Optional(userRole);

            testBed.Fake<IUserRoleStorage>()
                .Setup(s => s.GetUserRoleById(It.IsAny<string>()))
                .ReturnsAsync(userRoleOpt);
            var userRoleService = testBed.CreateSubject();
            
            // Act
            var result = await userRoleService.GetUserRoleById(roleId);
            
            // Assert
            result.Should().Be(userRoleOpt);
            result.IsSome.Should().Be(true);
            result.Get().Should().Be(userRole);
        }

        [Fact]
        public async Task UserRoleServiceTest_UpdateUserRoleWithExist_ShouldCallUpdateDocumentAndReturnUpdateStateUpdated()
        {
            // Arrange
            const string roleId = "Bson-Id";
            const string roleDescription = "test_role";
            var userRole = new UserRole
            {
                Id = roleId,
                Role = roleDescription,
                Policy = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version}
            };
            var userRoleOpt = Optional(userRole);

            var userRoleStorage = new Mock<IUserRoleStorage>();
            userRoleStorage.Setup(s => s.GetUserRole(It.IsAny<string>()))
                .ReturnsAsync(userRoleOpt);
            userRoleStorage.Setup(s =>
                    s.UpdateDocument(It.IsAny<UserRole>(), It.IsAny<Expression<Func<UserRole, bool>>>()))
                .ReturnsAsync(userRole);
            testBed.RegisterSingleton(userRoleStorage.Object);
            var userRoleService = testBed.CreateSubject();
            
            // Act
            var result = await userRoleService.UpdateUserRole(userRole);
            
            // Assert
            userRoleStorage.Verify(v => v.UpdateDocument(It.IsAny<UserRole>(),
                It.IsAny<Expression<Func<UserRole, bool>>>()), Times.Once);

            result.IsSuccess.Should().Be(true);
            result.GetSuccess().Should().Be(UpdateState.Updated);
        }

        [Fact]
        public async Task UserRoleServiceTest_UpdateUserRoleWithoutExist_ShouldCallNewDocumentAndReturnUpdateStateUpdated()
        {
            // Arrange
            const string roleId = "Bson-Id";
            const string roleDescription = "test_role";
            var userRole = new UserRole
            {
                Id = roleId,
                Role = roleDescription,
                Policy = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version}
            };
            var userRoleOpt = Option<UserRole>.None();

            var userRoleStorage = new Mock<IUserRoleStorage>();
            userRoleStorage.Setup(s => s.GetUserRole(It.IsAny<string>()))
                .ReturnsAsync(userRoleOpt);
            userRoleStorage.Setup(s =>
                    s.NewDocument(It.IsAny<UserRole>()))
                .ReturnsAsync(userRole);
            testBed.RegisterSingleton(userRoleStorage.Object);
            var userRoleService = testBed.CreateSubject();
            
            // Act
            var result = await userRoleService.UpdateUserRole(userRole);
            
            // Assert
            userRoleStorage.Verify(v => v.NewDocument(It.IsAny<UserRole>()), Times.Once);

            result.IsSuccess.Should().Be(true);
            result.GetSuccess().Should().Be(UpdateState.Updated);
        }

        [Fact]
        public async Task UserRoleServiceTest_DeleteUserRoleWithExist_ShouldCallDeleteDocumentAndReturnUpdateStateDeleted()
        {
            // Arrange
            const string roleId = "Bson-Id";
            const string roleDescription = "test_role";
            var userRole = new UserRole
            {
                Id = roleId,
                Role = roleDescription,
                Policy = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version}
            };
            var userRoleOpt = Optional(userRole);

            var userRoleStorage = new Mock<IUserRoleStorage>();
            userRoleStorage.Setup(s => s.GetUserRole(It.IsAny<string>()))
                .ReturnsAsync(userRoleOpt);
            userRoleStorage.Setup(s =>
                    s.DeleteDocument(It.IsAny<string>(), It.IsAny<Expression<Func<UserRole, bool>>>()))
                .ReturnsAsync(userRole);
            testBed.RegisterSingleton(userRoleStorage.Object);
            var userRoleService = testBed.CreateSubject();
            
            // Act
            var result = await userRoleService.DeleteUserRole(userRole);
            
            // Assert
            userRoleStorage.Verify(v => v.DeleteDocument(It.IsAny<string>(),
                It.IsAny<Expression<Func<UserRole, bool>>>()), Times.Once);

            result.IsSuccess.Should().Be(true);
            result.GetSuccess().Should().Be(UpdateState.Deleted);
        }

        [Fact]
        public async Task UserRoleServiceTest_DeleteUserRoleWithoutExist_ShouldReturnUpdateStateIgnore()
        {
            // Arrange
            const string roleId = "Bson-Id";
            const string roleDescription = "test_role";
            var userRole = new UserRole
            {
                Id = roleId,
                Role = roleDescription,
                Policy = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version}
            };
            var userRoleOpt = Option<UserRole>.None();

            testBed.Fake<IUserRoleStorage>()
                .Setup(s => s.GetUserRole(It.IsAny<string>()))
                .ReturnsAsync(userRoleOpt);
            var userRoleService = testBed.CreateSubject();
            
            // Act
            var result = await userRoleService.DeleteUserRole(userRole);
            
            // Assert
            result.IsSuccess.Should().Be(true);
            result.GetSuccess().Should().Be(UpdateState.Ignore);
        }
    }
}