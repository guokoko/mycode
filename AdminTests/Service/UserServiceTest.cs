using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Admin.Services;
using CTO.Price.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using RZ.Foundation;
using Xunit;
using static RZ.Foundation.Prelude;

namespace AdminTests.Service
{
    public class UserServiceTest
    {
        [Fact]
        public async Task AuditService_GetAllUser_ReturnSameObject()
        {
            //Arrange
            List<ApplicationUser> users = new List<ApplicationUser>();

            var userStorage = new Mock<IUserStorage>();
            userStorage.Setup(p => p.GetAllUser()).ReturnsAsync(users);
            var userRoleStorage = new Mock<IUserRoleStorage>();
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);
            var service = new UserService(userStorage.Object, userRoleStorage.Object, userManager.Object);

            //Act
            var result = await service.GetAllUser();

            //Assert
            result.Should().Equal(users);
        }
        
        [Fact]
        public async Task AuditService_GetUserByEmail_ReturnSameEmail()
        {
            //Arrange
            var user = new ApplicationUser()
            {
                UserName = "test_user",
                Email = "abc@mail.com",
                Roles = new List<string>{"5f8424b843286b765210a13f"}
            };
            

            var userStorage = new Mock<IUserStorage>();
            userStorage.Setup(p => p.GetUserByEmail(user.Email)).ReturnsAsync(user);
            var userRoleStorage = new Mock<IUserRoleStorage>();
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);
            
            var service = new UserService(userStorage.Object, userRoleStorage.Object, userManager.Object);

            //Act
            var result = await service.GetUserByEmail(user.Email);

            //Assert
            result.Get().Email.Should().Be(user.Email);
        }
        
        [Fact]
        public async Task AuditService_UpdateUser_ReturnUpdatedState_UpdateCase()
        {
            //Arrange
            var updated = UpdateState.Updated;
            var user = new ApplicationUser()
            {
                UserName = "test_user",
                Email = "abc@mail.com",
                Roles = new List<string>{"5f8424b843286b765210a13f"}
            };

            var userStorage = new Mock<IUserStorage>();

            userStorage.Setup(p => p.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(user);
            userStorage.Setup(p => p.UpdateDocument(It.IsAny<ApplicationUser>(), It.IsAny<Expression<Func<ApplicationUser, bool>>>())).ReturnsAsync(new ApplicationUser());
            var userRoleStorage = new Mock<IUserRoleStorage>();
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);
            var service = new UserService(userStorage.Object, userRoleStorage.Object, userManager.Object);

            //Act
            var result = await service.UpdateUser(user);

            //Assert
            result.GetSuccess().Should().Be(updated);
        }
        
        [Fact]
        public async Task AuditService_UpdateUser_ReturnUpdatedState_AddNewCase()
        {
            //Arrange
            var updated = UpdateState.Updated;
            var user = new ApplicationUser()
            {
                UserName = "test_user",
                Email = "abc@mail.com",
                Roles = new List<string>{"5f8424b843286b765210a13f"}
            };

            var userStorage = new Mock<IUserStorage>();
            
            userStorage.Setup(p => p.NewDocument(user)).ReturnsAsync(user);
            var userRoleStorage = new Mock<IUserRoleStorage>();
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);
            
            var service = new UserService(userStorage.Object, userRoleStorage.Object, userManager.Object);

            //Act
            var result = await service.UpdateUser(user);

            //Assert
            result.GetSuccess().Should().Be(updated);
        }
        
        [Fact]
        public async Task AuditService_DeleteUser_ReturnDeletedState()
        {
            //Arrange
            var deleted = UpdateState.Deleted;
            var user = new ApplicationUser()
            {
                UserName = "test_user",
                Email = "abc@mail.com",
                Roles = new List<string>{"5f8424b843286b765210a13f"}
            };

            var userStorage = new Mock<IUserStorage>();

            userStorage.Setup(p => p.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(user);
            userStorage.Setup(p => p.DeleteDocument(It.IsAny<string>(), It.IsAny<Expression<Func<ApplicationUser, bool>>>())).ReturnsAsync(new ApplicationUser());
            var userRoleStorage = new Mock<IUserRoleStorage>();
            
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);
            var service = new UserService(userStorage.Object, userRoleStorage.Object, mockUserManager.Object);

            //Act
            var result = await service.DeleteUser(user);

            //Assert
            result.GetSuccess().Should().Be(deleted);
        }

        [Fact]
        public async Task UserServiceTest_GetAllUserRoleDetail_ReturnUserRoleViewModelList()
        {
            // Arrange
            var user = new ApplicationUser()
            {
                UserName = "test_user",
                Email = "abc@mail.com",
                Roles = new List<string>{"5f8424b843286b765210a13f"}
            };
            var users = new List<ApplicationUser> {user};

            var userRole = (new UserRole
            {
                Id = "Bson-Id",
                Role = "test_role",
                Policy = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version}
            }).ToOption();
        
            var userStorage = new Mock<IUserStorage>();
            userStorage.Setup(p => p.GetAllUser()).ReturnsAsync(users);
            
            var userRoleStorage = new Mock<IUserRoleStorage>();
            userRoleStorage.Setup(s => s.GetUserRoleById(It.IsAny<string>())).ReturnsAsync(userRole);
            
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);
            var userService = new UserService(userStorage.Object, userRoleStorage.Object, mockUserManager.Object);
            
            // Act
            var result = await userService.GetAllUserRoleDetail();
            
            // Assert
            result.Count.Should().Be(1);
            result[0].User.Should().Be(user);
            result[0].UserRole.Should().Be(userRole.Get());
        }

        [Fact]
        public async Task UserServiceTest_GetUserByName_ShouldReturnApplicationUserOption()
        {
            // Arrange
            const string userName = "test_user";
            var user = new ApplicationUser()
            {
                UserName = userName,
                Email = "abc@mail.com",
                Roles = new List<string>{"5f8424b843286b765210a13f"}
            };
            var userOpt = Optional(user);
            
            var userStorage = new Mock<IUserStorage>();
            userStorage.Setup(p => p.GetUserByName(It.IsAny<string>())).ReturnsAsync(userOpt);
            
            var userRoleStorage = new Mock<IUserRoleStorage>();
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);
            var userService = new UserService(userStorage.Object, userRoleStorage.Object, mockUserManager.Object);
            
            // Act
            var result = await userService.GetUserByName(userName);
            
            // Assert
            result.Should().Be(userOpt);
            result.IsSome.Should().Be(true);
            result.Get().Should().Be(user);
        }

        [Fact]
        public async Task UserServiceTest_GetTotalRecordWithFilter_ShouldReturnUserRoleCount()
        {
            // Arrange
            const string userName = "test_user";
            const string email = "abc@mail.com";
            const string roleDescription = "test_role";
            const string employeeType = "test type";
            var logins = new IdentityUserLogin<string>
            {
                LoginProvider = employeeType
            };
            var user = new ApplicationUser()
            {
                UserName = userName,
                NormalizedUserName = userName.ToUpper(),
                Email = email,
                NormalizedEmail = email.ToUpper(),
                Roles = new List<string>{"5f8424b843286b765210a13f"},
                Logins = new List<IdentityUserLogin<string>> {logins}
            };
            var users = new List<ApplicationUser> {user};

            var userRole = (new UserRole
            {
                Id = "Bson-Id",
                Role = roleDescription,
                Policy = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version}
            }).ToOption();
        
            var userStorage = new Mock<IUserStorage>();
            userStorage.Setup(p => p.GetAllUser()).ReturnsAsync(users);
            
            var userRoleStorage = new Mock<IUserRoleStorage>();
            userRoleStorage.Setup(s => s.GetUserRoleById(It.IsAny<string>())).ReturnsAsync(userRole);
            
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);
            var userService = new UserService(userStorage.Object, userRoleStorage.Object, mockUserManager.Object);
            
            // Act
            var result = await userService.GetTotalRecordWithFilter(email, 
                roleDescription, employeeType);
            
            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task UserServiceTest_GetUserByEmailRoleProvider_ShouldReturnUserRoleViewModelList()
        {
            // Arrange
            const string userName = "test_user";
            const string email = "abc@mail.com";
            const string roleDescription = "test_role";
            const string employeeType = "test type";
            var logins = new IdentityUserLogin<string>
            {
                LoginProvider = employeeType
            };
            var user = new ApplicationUser()
            {
                UserName = userName,
                NormalizedUserName = userName.ToUpper(),
                Email = email,
                NormalizedEmail = email.ToUpper(),
                Roles = new List<string>{"5f8424b843286b765210a13f"},
                Logins = new List<IdentityUserLogin<string>> {logins}
            };
            var users = new List<ApplicationUser> {user};

            var userRole = (new UserRole
            {
                Id = "Bson-Id",
                Role = roleDescription,
                Policy = new[] {RolePolicy.Home, RolePolicy.Upload, RolePolicy.Version}
            }).ToOption();
        
            var userStorage = new Mock<IUserStorage>();
            userStorage.Setup(p => p.GetAllUser()).ReturnsAsync(users);
            
            var userRoleStorage = new Mock<IUserRoleStorage>();
            userRoleStorage.Setup(s => s.GetUserRoleById(It.IsAny<string>())).ReturnsAsync(userRole);
            
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);
            var userService = new UserService(userStorage.Object, userRoleStorage.Object, mockUserManager.Object);
            
            // Act
            var result = await userService.GetUserByEmailRoleProvider(email, roleDescription, 
                employeeType, 1, 10);
            
            // Assert
            result.Count.Should().Be(1);
            result[0].User.Should().Be(user);
            result[0].UserRole.Should().Be(userRole.Get());
        }

        [Fact]
        public async Task UserServiceTest_CreateApplicationUserWithPassword_ShouldCallCreateAsync()
        {
            // Arrange
            const string userName = "test_user";
            const string email = "abc@mail.com";
            const string password = "P@ssw0rd4Test";
            var user = new ApplicationUser()
            {
                UserName = userName,
                NormalizedUserName = userName.ToUpper(),
                Email = email,
                NormalizedEmail = email.ToUpper(),
                Roles = new List<string>{"5f8424b843286b765210a13f"},
            };

            var userStorage = new Mock<IUserStorage>();
            var userRoleStorage = new Mock<IUserRoleStorage>();
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);
            
            var userService = new UserService(userStorage.Object, userRoleStorage.Object, mockUserManager.Object);
            
            // Act
            await userService.CreateApplicationUserWithPassword(user, password);
            
            // Assert
            mockUserManager.Verify(v => v.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UserServiceTest_UpdateSecurityStampInternal_ShouldCallUpdateSecurityStampAsync()
        {
            // Arrange
            const string userName = "test_user";
            const string email = "abc@mail.com";
            var user = new ApplicationUser()
            {
                UserName = userName,
                NormalizedUserName = userName.ToUpper(),
                Email = email,
                NormalizedEmail = email.ToUpper(),
                Roles = new List<string>{"5f8424b843286b765210a13f"},
            };

            var userStorage = new Mock<IUserStorage>();
            var userRoleStorage = new Mock<IUserRoleStorage>();
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null, null, null, null, null, null, null, null);
            
            var userService = new UserService(userStorage.Object, userRoleStorage.Object, mockUserManager.Object);
            
            // Act
            await userService.UpdateSecurityStampInternal(user);
            
            // Assert
            mockUserManager.Verify(v => v.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>()), Times.Once);
        }
    }
}