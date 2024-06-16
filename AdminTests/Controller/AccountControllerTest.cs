using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CTO.Price.Admin.Controllers;
using CTO.Price.Admin.Data;
using CTO.Price.Admin.Services;
using CTO.Price.Shared;
using CTO.Price.Shared.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using RZ.Foundation;
using TestUtility;
using Xunit;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace AdminTests.Controller
{
    public class AccountControllerTest
    {
        #region Logout
        [Fact]
        public async Task AccountController_Logout_LogoutSuccessByCGEmployee()
        {
            //Arrange
            var mockHostValue = "test-host/";
            var mockScheme = "testScheme";
            var mockIdToken = "TestIdToken";
            var mockRemoteSignOutPath = "testRemoteSignOutPath";
            var mockSignedOutCallbackPath = "testSignedOutCallbackPath";
            var redirectUrl = $"{mockRemoteSignOutPath}?post_logout_redirect_uri={mockScheme}://{mockHostValue}{mockSignedOutCallbackPath}/&id_token_hint={mockIdToken}";
            var mockCookieKey = "testCookieKey";
            var mockCookieValue = "testCookieValue";
            
            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();

            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = mockRemoteSignOutPath,
                SignedOutCallbackPath = mockSignedOutCallbackPath
            });
            
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(_ => _.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.FromResult((object)null));
            
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(_ => _.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);
            
            var userClaim = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.AuthenticationMethod, AzureADDefaults.AuthenticationScheme),
                new Claim("preferred_username", "testEmail")
            },"TestAuthentication"));
            
            var mockSession = new Mock<ISession>();
            var mockSessionKey = "id_token";
            byte[] mockSessionValue = System.Text.Encoding.UTF8.GetBytes(mockIdToken);
            mockSession.Setup(_ => _.Set(mockSessionKey, It.IsAny<byte[]>())).Callback<string, byte[]>((k,v) => mockSessionValue = v);
            mockSession.Setup(_ => _.TryGetValue(mockSessionKey, out mockSessionValue)).Returns(true);
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();
            accountController.ControllerContext.HttpContext = new DefaultHttpContext { Session = mockSession.Object, User = userClaim, RequestServices = serviceProviderMock.Object};
            HostString mockHost = new HostString(mockHostValue);
            accountController.ControllerContext.HttpContext.Request.Host = mockHost;
            accountController.ControllerContext.HttpContext.Request.Scheme = mockScheme;
            accountController.ControllerContext.HttpContext.Request.Cookies = MockRequestCookieCollection(mockCookieKey, mockCookieValue);
            
            // Act
            var signInResult = await accountController.Logout();
            
            // Assert
            var logOUtRedirectResult = ((RedirectResult)signInResult).Url;
            logOUtRedirectResult.Should().Be(redirectUrl);
        }
        
        [Fact]
        public async Task AccountController_Logout_LogoutSuccessByNonCGEmployee()
        {
            //Arrange
            var mockCookieKey = "testCookieKey";
            var mockCookieValue = "testCookieValue";
            
            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();
            
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>()
            });

            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();
            accountController.ControllerContext.HttpContext = new DefaultHttpContext { User = null};
            accountController.ControllerContext.HttpContext.Request.Cookies = MockRequestCookieCollection(mockCookieKey, mockCookieValue);
            
            // Act
            var signInResult = await accountController.Logout();
            
            // Assert
            var logOUtRedirectResult = ((RedirectResult)signInResult).Url;
            logOUtRedirectResult.Should().Be("/");
        }
        #endregion
        
        #region Login
        [Fact]
        public async Task AccountController_Login_ShouldBeSuccess()
        {
            //Arrange
            var auditLogActionType = AuditLogActionType.Logout;
            var logTime = new DateTime(2020, 12, 29).ToUniversalTime();
            
            var userLogin = new LoginViewModel()
            {
                UserEmail = "abc@mail.com",
                Password = "Abc@123456"
            };
            var user = new ApplicationUser()
            {
                UserName = "abc",
                Email = "abc@mail.com",
                Roles = new List<string>{"5f8424b843286b765210a13f"}
            };
            var redirectResult = "/";
            
            var mockSignInManager = new Mock<SignInManagerMock>();
            mockSignInManager.Setup(
                x => x.PasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>(),
                    It.IsAny<bool>())).ReturnsAsync(SignInResult.Success);
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            mockIAuditService.Setup(s => s.CreateLogMessage(userLogin.UserEmail, auditLogActionType, logTime));
            var mockIUserService = new Mock<IUserService>();
            mockIUserService.Setup(s => s.GetUserByEmail(userLogin.UserEmail)).ReturnsAsync(user);
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });

            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();

            // Act
            var signInResult = await accountController.Login(userLogin);
            
            // Assert
            var signInRedirectResult = ((RedirectResult)signInResult).Url;
            signInRedirectResult.Should().Be(redirectResult);
        }
        
        [Fact]
        public async Task AccountController_Login_ShouldReturnLoginViewModelWhenLoginFail()
        {
            //Arrange
            var auditLogActionType = AuditLogActionType.Logout;
            var logTime = new DateTime(2020, 12, 29).ToUniversalTime();
            
            var userLogin = new LoginViewModel()
            {
                UserEmail = "abc@mail.com",
                Password = "Abc@123456",
                RememberMe = false
            };
            var user = new ApplicationUser()
            {
                UserName = "abc",
                Email = "abc@mail.com",
                Roles = new List<string>{"5f8424b843286b765210a13f"}
            };

            var mockSignInManager = new Mock<SignInManagerMock>();
            mockSignInManager.Setup(
                x => x.PasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>(),
                    It.IsAny<bool>())).ReturnsAsync(SignInResult.Failed);
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            mockIAuditService.Setup(s => s.CreateLogMessage(userLogin.UserEmail, auditLogActionType, logTime));
            var mockIUserService = new Mock<IUserService>();
            mockIUserService.Setup(s => s.GetUserByEmail(userLogin.UserEmail)).ReturnsAsync(user);
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();
            var mockTempData = new Mock<ITempDataDictionary>();
            accountController.TempData = mockTempData.Object;
            
            // Act
            var signInResult = await accountController.Login(userLogin);
            
            // Assert
            var signInViewResult = (ViewResult)signInResult;
            signInViewResult.Model.Should().Be(userLogin);
        }
        
        [Fact]
        public async Task AccountController_Login_ShouldReturnLoginViewModelWhenModelStateInvalid()
        {
            //Arrange
            var userLogin = new LoginViewModel()
            {
                UserEmail = "abc@mail.com",
                Password = "Abc@123456",
                RememberMe = false
            };

            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ModelState.AddModelError("key", "error message");
            
            // Act
            var signInResult = await accountController.Login(userLogin);
            
            // Assert
            var signInViewResult = (ViewResult)signInResult;
            signInViewResult.Model.Should().Be(userLogin);
        }
        
        [Fact]
        public async Task AccountController_Login_ShouldReturnEmptyObjectWhenRedirectToLoginSuccess()
        {
            //Arrange
            var email = "abc@mail.com";
            var redirectResult = "/";
            
            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();
            
            var userClaim = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.AuthenticationMethod, AzureADDefaults.AuthenticationScheme),
                new Claim("preferred_username", email)
            },"TestAuthentication"));
            accountController.ControllerContext.HttpContext = new DefaultHttpContext { User = userClaim};

            // Act
            var signInResult = await accountController.Login();
            
            // Assert
            var signInRedirectResult = ((RedirectResult)signInResult).Url;
            signInRedirectResult.Should().Be(redirectResult);
        }
        
        [Fact]
        public async Task AccountController_Login_ShouldReturnEmptyObjectWhenRedirectToLoginFail()
        {
            //Arrange
            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();

            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(_ => _.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.FromResult((object)null));
            
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(_ => _.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);
            
            accountController.ControllerContext.HttpContext = new DefaultHttpContext { User = null, RequestServices = serviceProviderMock.Object};
            
            var mockTempData = new Mock<ITempDataDictionary>();
            accountController.TempData = mockTempData.Object;

            // Act
            var signInResult = await accountController.Login();
            
            // Assert
            var signInViewResult = (ViewResult)signInResult;
            signInViewResult.Model.Should().Be(null);
        }
        #endregion
        
        #region Register
        [Fact]
        public async Task AccountController_Register_ShouldReturnHomePageWhenExistedUser()
        {
            //Arrange
            var user = new ApplicationUser()
            {
                UserName = "abc",
                Email = "abc@mail.com",
                Roles = new List<string>{"5f8424b843286b765210a13f"}
            };
            var redirectUrl = "/";

            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();
            mockIUserService.Setup(s => s.GetAllUser()).ReturnsAsync(new List<ApplicationUser>{user});
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();

            // Act
            var signInResult = await accountController.Register();
            
            // Assert
            var signInRedirectResult = (RedirectResult)signInResult;
            signInRedirectResult.Url.Should().Be(redirectUrl);
        }
        
        [Fact]
        public async Task AccountController_Register_ShouldReturnLoginPageWhenNotExistAnyUser()
        {
            //Arrange
            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();
            mockIUserService.Setup(s => s.GetAllUser()).ReturnsAsync(new List<ApplicationUser>());
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();

            // Act
            var signInResult = await accountController.Register();
            
            // Assert
            var signInViewResult = (ViewResult)signInResult;
            signInViewResult.Model.Should().Be(null);
        }
        
        [Fact]
        public async Task AccountController_Register_ShouldBeSuccess()
        {
            //Arrange
            var userRole = new UserRole()
            {
                Id = "abc123",
                Role = "admin"
            };

            var userModel = new ExternalLoginModel
            {
                Email = "abc@mail.com"
            };
            
            
            var user = new ApplicationUser
            {
                UserName = "abc",
                NormalizedUserName = "ABC",
                Email = "abc@mail.com", 
                NormalizedEmail = "ABC@MAIL.COM",
                Roles = new List<string>{userRole.Role},
                Logins = new List<IdentityUserLogin<string>>
                {
                    new IdentityUserLogin<string>
                    {
                        LoginProvider = AzureADDefaults.AuthenticationScheme,
                        ProviderDisplayName = AzureADDefaults.DisplayName
                    }
                }
            };
            
            var redirectResult = "/";
            
            var mockSignInManager = new Mock<SignInManagerMock>();
            mockSignInManager.Setup(
                x => x.PasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>(),
                    It.IsAny<bool>())).ReturnsAsync(SignInResult.Success);
            var mockIUserRoleService = new Mock<IUserRoleService>();
            mockIUserRoleService.Setup(s => s.GetUserRole(It.IsAny<string>())).ReturnsAsync(new UserRole().ToOption());
            mockIUserRoleService.Setup(s => s.UpdateUserRole(It.IsAny<UserRole>())).ReturnsAsync(new ApiResult<UpdateState>());

            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();
            mockIUserService.Setup(s => s.UpdateUser(user)).ReturnsAsync(new ApiResult<UpdateState>());
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();
            var mockTempData = new Mock<ITempDataDictionary>();
            accountController.TempData = mockTempData.Object;
            
            // Act
            var signInResult = await accountController.Register(userModel);
            
            // Assert
            var signInRedirectResult = ((RedirectResult)signInResult).Url;
            signInRedirectResult.Should().Be(redirectResult);
        }
        
        [Fact]
        public async Task AccountController_Register_ShouldReturnLoginViewModelWhenModelStateInvalid()
        {
            //Arrange
            var userModel = new ExternalLoginModel
            {
                Email = "abc@mail.com"
            };

            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ModelState.AddModelError("key", "error message");
            
            // Act
            var signInResult = await accountController.Register(userModel);
            
            // Assert
            var registerViewResult = (ViewResult)signInResult;
            registerViewResult.Model.Should().Be(null);
        }
        
        [Fact]
        public async Task AccountController_Register_ShouldReturnLoginWhenInitAdminRoleFail()
        {
            //Arrange
            var userModel = new ExternalLoginModel
            {
                Email = "abc@mail.com"
            };

            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            mockIUserRoleService.Setup(s => s.GetUserRole(It.IsAny<string>())).ReturnsAsync(new UserRole().ToOption());

            mockIUserRoleService.Setup(s => s.UpdateUserRole(It.IsAny<UserRole>())).ReturnsAsync(new ApiResult<UpdateState>(new Exception()));
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();
            var mockTempData = new Mock<ITempDataDictionary>();
            accountController.TempData = mockTempData.Object;
            
            // Act
            var signInResult = await accountController.Register(userModel);
            
            // Assert
            var registerViewResult = (ViewResult)signInResult;
            registerViewResult.Model.Should().Be(null);
        }
        
        [Fact]
        public async Task AccountController_Register_ReturnNullWhenRegisterFail()
        {
            //Arrange
            var userModel = new ExternalLoginModel
            {
                Email = "abc@mail.com"
            };

            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();
            
            mockSignInManager.Setup(
                x => x.PasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>(),
                    It.IsAny<bool>())).ReturnsAsync(SignInResult.Success);
            mockIUserRoleService.Setup(s => s.GetUserRole(It.IsAny<string>())).ReturnsAsync(new UserRole().ToOption());
            mockIUserRoleService.Setup(s => s.UpdateUserRole(It.IsAny<UserRole>())).ReturnsAsync(new ApiResult<UpdateState>());
            mockIUserService.Setup(s => s.UpdateUser(It.IsAny<ApplicationUser>())).ReturnsAsync(new ApiResult<UpdateState>(new Exception()));
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();

            // Act
            var signInResult = await accountController.Register(userModel);
            
            // Assert
            var registerViewResult = (ViewResult)signInResult;
            registerViewResult.Model.Should().Be(null);
        }
        #endregion

        #region AzureLogin
        [Fact]
        public void AccountController_AzureLogin_ReturnNullWhenRegisterFail()
        {
            //Arrange
            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();
            
            mockSignInManager.Setup(
                x => x.PasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>(),
                    It.IsAny<bool>())).ReturnsAsync(SignInResult.Success);
            mockIUserRoleService.Setup(s => s.GetUserRole(It.IsAny<string>())).ReturnsAsync(new UserRole().ToOption());
            mockIUserRoleService.Setup(s => s.UpdateUserRole(It.IsAny<UserRole>())).ReturnsAsync(new ApiResult<UpdateState>());
            mockIUserService.Setup(s => s.UpdateUser(It.IsAny<ApplicationUser>())).ReturnsAsync(new ApiResult<UpdateState>(new Exception()));

            var urlCallBack = "Account/AzureLoginCallback?returnUrl=/";
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.SetReturnsDefault(urlCallBack);

            var authenticationProperty = new AuthenticationProperties
            {
                RedirectUri = urlCallBack,
                Items = {{"LoginProvider", "AzureAD"}}
            };

            mockSignInManager.Setup(s => s.ConfigureExternalAuthenticationProperties(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(authenticationProperty);
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();
            accountController.Url = mockUrlHelper.Object;
            
            // Act
            var azureSignInResult = accountController.AzureLogin(It.IsAny<string>());
            
            // Assert
            var result = (ChallengeResult)azureSignInResult;
            result.Properties.Should().Be(authenticationProperty);
        }
        
        [Fact]
        public async Task AccountController_AzureLoginCallback_ReturnHomePageWhenSignInSuccess()
        {
            //Arrange
            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();

            var redirectUrl = "/";
            var userClaim = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.Upn, "testEmail")
            },"TestAuthentication"));

            var externalLoginInfo = new ExternalLoginInfo(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
            {
                AuthenticationTokens = new List<AuthenticationToken> {new AuthenticationToken(){ Name = "id_token", Value = "testIdToken"}},
                ProviderKey = "testProviderKey",
                LoginProvider = "AzureAD",
                Principal = userClaim
            };
            
            mockSignInManager.Setup(x => x.GetExternalLoginInfoAsync(It.IsAny<string>())).ReturnsAsync(externalLoginInfo);
            
            var mockSession = new Mock<ISession>();
            var mockSessionKey = "id_token";
            
            byte[] mockSessionValue = System.Text.Encoding.UTF8.GetBytes("TestIdToken");
            mockSession.Setup(_ => _.Set(mockSessionKey, It.IsAny<byte[]>())).Callback<string, byte[]>((k,v) => mockSessionValue = v);
            mockSession.Setup(_ => _.TryGetValue(mockSessionKey, out mockSessionValue)).Returns(true);
            
            var user = new ApplicationUser
            {
                Logins = new List<IdentityUserLogin<string>>{new IdentityUserLogin<string>{ProviderKey = string.Empty}}
            };
            
            mockIUserService.Setup(s => s.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(user.ToOption());
            mockIUserService.Setup(s => s.UpdateSecurityStampInternal(user));
            mockIUserService.Setup(s => s.UpdateUser(user)).ReturnsAsync(new ApiResult<UpdateState>());

            mockSignInManager.Setup(s => s.ExternalLoginSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Success);
            mockIAuditService.Setup(s => s.CreateLogMessage(It.IsAny<string>(), It.IsAny<AuditLogActionType>(), It.IsAny<DateTime>()));
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();
            accountController.ControllerContext.HttpContext = new DefaultHttpContext { Session = mockSession.Object};
            
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(s => s.IsLocalUrl(It.IsAny<string>())).Returns(true);
            accountController.Url = mockUrlHelper.Object;
            
            // Act
            var azureSignInResult = await accountController.AzureLoginCallback(redirectUrl);
            
            // Assert
            var result = (RedirectResult)azureSignInResult;
            result.Url.Should().Be(redirectUrl);
        }
        
        [Fact]
        public async Task AccountController_AzureLoginCallback_ReturnLoginPageWhenExternalLoginNull()
        {
            //Arrange
            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();

            mockSignInManager.Setup(x => x.GetExternalLoginInfoAsync(It.IsAny<string>())).ReturnsAsync(It.IsAny<ExternalLoginInfo>());
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = It.IsAny<string>(),
                SignedOutCallbackPath = It.IsAny<string>(),
            });
            
            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            // Act
            var azureSignInResult = await accountController.AzureLoginCallback(It.IsAny<string>());
            
            // Assert
            var result = (RedirectToActionResult)azureSignInResult;
            result.ActionName.Should().Be("Login");
        }
        
        [Fact]
        public async Task AccountController_AzureLoginCallback_ReturnRedirectUrlWhenAccountUnauthorized()
        {
            //Arrange
            var mockHostValue = "test-host/";
            var mockScheme = "testScheme";
            var mockIdToken = "TestIdToken";
            var mockRemoteSignOutPath = "testRemoteSignOutPath";
            var mockSignedOutCallbackPath = "testSignedOutCallbackPath";
            var redirectUrl = $"{mockRemoteSignOutPath}?post_logout_redirect_uri={mockScheme}://{mockHostValue}{mockSignedOutCallbackPath}/&id_token_hint={mockIdToken}";
            var mockCookieKey = "testCookieKey";
            var mockCookieValue = "testCookieValue";
            
            var mockAzureAdSetting = MockUtils.MockOption(new AzureAdSetting
            {
                Instance = It.IsAny<string>(),
                CallbackPath = It.IsAny<string>(),
                ClientId = It.IsAny<string>(),
                ClientSecret = It.IsAny<string>(),
                CookieSchemeName = It.IsAny<string>(),
                RemoteSignOutPath = mockRemoteSignOutPath,
                SignedOutCallbackPath = mockSignedOutCallbackPath,
            });
            
            var mockSignInManager = new Mock<SignInManagerMock>();
            var mockIUserRoleService = new Mock<IUserRoleService>();
            var mockIAuditService = new Mock<IAuditService>();
            var mockIUserService = new Mock<IUserService>();

            var userClaim = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.Upn, "testEmail")
            },"TestAuthentication"));
            
            var externalLoginInfo = new ExternalLoginInfo(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
            {
                AuthenticationTokens = new List<AuthenticationToken> {new AuthenticationToken(){ Name = "id_token", Value = mockIdToken}},
                ProviderKey = "testProviderKey",
                LoginProvider = "AzureAD",
                Principal = userClaim
            };
            mockSignInManager.Setup(x => x.GetExternalLoginInfoAsync(It.IsAny<string>())).ReturnsAsync(externalLoginInfo);
            
            var mockSession = new Mock<ISession>();
            var mockSessionKey = "id_token";
            byte[] mockSessionValue = System.Text.Encoding.UTF8.GetBytes(mockIdToken);
            mockSession.Setup(_ => _.Set(mockSessionKey, It.IsAny<byte[]>())).Callback<string, byte[]>((k,v) => mockSessionValue = v);
            mockSession.Setup(_ => _.TryGetValue(mockSessionKey, out mockSessionValue)).Returns(true);
            
            mockIUserService.Setup(s => s.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(It.IsAny<ApplicationUser>().ToOption());
            mockIAuditService.Setup(s => s.CreateLogMessage(It.IsAny<string>(), It.IsAny<AuditLogActionType>(), It.IsAny<DateTime>()));
            
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(_ => _.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.FromResult((object)null));
            
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(_ => _.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            var accountController = new AccountController(mockSignInManager.Object, mockIUserRoleService.Object, mockIAuditService.Object, mockIUserService.Object, mockAzureAdSetting.Object);
            accountController.ControllerContext = new ControllerContext();
            accountController.ControllerContext.HttpContext = new DefaultHttpContext { Session = mockSession.Object, User = null, RequestServices = serviceProviderMock.Object};
            var mockTempData = new Mock<ITempDataDictionary>();
            accountController.TempData = mockTempData.Object;
            HostString mockHost = new HostString(mockHostValue);
            accountController.ControllerContext.HttpContext.Request.Host = mockHost;
            accountController.ControllerContext.HttpContext.Request.Scheme = mockScheme;
            accountController.ControllerContext.HttpContext.Request.Cookies = MockRequestCookieCollection(mockCookieKey, mockCookieValue);
            
            // Act
            var azureSignInResult = await accountController.AzureLoginCallback(It.IsAny<string>());
            
            // Assert
            var result = (RedirectResult)azureSignInResult;
            result.Url.Should().Be(redirectUrl);
        }
        
        private static IRequestCookieCollection MockRequestCookieCollection(string key, string value)
        {
            var requestFeature = new HttpRequestFeature();
            var featureCollection = new FeatureCollection();

            requestFeature.Headers = new HeaderDictionary();
            requestFeature.Headers.Add(HeaderNames.Cookie, new StringValues(key + "=" + value));

            featureCollection.Set<IHttpRequestFeature>(requestFeature);

            var cookiesFeature = new RequestCookiesFeature(featureCollection);

            return cookiesFeature.Cookies;
        }
        #endregion
    }
}