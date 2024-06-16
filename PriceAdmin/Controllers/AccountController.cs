 using System;
 using System.Collections.Generic;
 using System.Linq;
 using System.Security.Claims;
 using System.Security.Principal;
 using System.Threading.Tasks;
 using CTO.Price.Admin.Data;
 using CTO.Price.Admin.Services;
 using CTO.Price.Shared;
 using CTO.Price.Shared.Domain;
 using Microsoft.AspNetCore.Authentication;
 using Microsoft.AspNetCore.Authentication.AzureAD.UI;
 using Microsoft.AspNetCore.Authorization;
 using Microsoft.AspNetCore.Http;
 using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
 using Microsoft.Extensions.Options;
 using RZ.Foundation;

 namespace CTO.Price.Admin.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IUserRoleService userRoleService;
        private readonly IAuditService auditService;
        private readonly IUserService userService;
        AzureAdSetting azureAdSetting;
        
        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            IUserRoleService userRoleService, IAuditService auditService,
            IUserService userService, IOptionsMonitor<AzureAdSetting> azureAdSetting)
        {
            this.signInManager = signInManager;
            this.userRoleService = userRoleService;
            this.auditService = auditService;
            this.userService = userService;
            this.azureAdSetting = azureAdSetting.CurrentValue;
        }

        [HttpPost]        
        public async Task<IActionResult> Logout()
        {
            if (User.Identity is ClaimsIdentity identity && identity.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.AuthenticationMethod)?.Value == AzureADDefaults.AuthenticationScheme)
            {
                var url = await LogOutAzureUser();
                var email = identity.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ?? string.Empty;
                await auditService.CreateLogMessage(email, AuditLogActionType.Logout, DateTime.UtcNow);
                
                return Redirect(url);
            }
            else
            {
                ClearUserData();
                await signInManager.SignOutAsync();
                return Redirect("/");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                return View();
            }

            return Redirect("/");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                Microsoft.AspNetCore.Identity.SignInResult? result = null; 
                var user = (await userService.GetUserByEmail(model.UserEmail));
                if (user.IsSome)
                    result = await signInManager.PasswordSignInAsync(user.Get(), model.Password, false, false);

                if (result != null && result.Succeeded)
                {
                    await auditService.CreateLogMessage(model.UserEmail, AuditLogActionType.Login, DateTime.UtcNow);
                    return Redirect("/");
                }
                
                TempData["RedirectErrorMessage"] = "Invalid login attempt";
                return View(model);
            }

            return View(model);
        }
        
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            var users = await userService.GetAllUser();
            if(users.Count == 0)
                return View("Login", null);
            
            return Redirect("/");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(ExternalLoginModel model)
        {
            if (ModelState.IsValid)
            {
                var adminRole = await userRoleService.GetUserRole("admin");

                if (adminRole.IsNone || !adminRole.Get().Role.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    var initAdminRole = await InitialAdminAccount();
                    if(initAdminRole.IsSuccess)
                        adminRole = await userRoleService.GetUserRole("admin");
                    else
                    {
                        TempData["RedirectErrorMessage"] = "Register CG employee admin account fail";
                        return View("Login", null);
                    }
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email.Substring(0, model.Email.IndexOf('@')),
                    NormalizedUserName = model.Email.Substring(0, model.Email.IndexOf('@')).ToUpper(),
                    Email = model.Email, 
                    NormalizedEmail = model.Email.ToUpper(),
                    Roles = adminRole.IsSome ? new List<string>{adminRole.Get().Id} : null,
                    Logins = new List<IdentityUserLogin<string>>
                    {
                        new IdentityUserLogin<string>
                        {
                            LoginProvider = AzureADDefaults.AuthenticationScheme,
                            ProviderDisplayName = AzureADDefaults.DisplayName
                        }
                    }
                };

                var result = await userService.UpdateUser(user);
                if (result.IsSuccess)
                { 
                    TempData["RedirectSuccessMessage"] = "Your CG employee admin account has been successfully registered";
                    return Redirect("/");
                }
                
                ModelState.AddModelError(string.Empty, result.GetFail().Message);
            }

            return View("Login", null);
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult AzureLogin(string provider, string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            var redirectUrl = Url.Action(nameof(AzureLoginCallback), "Account", new { returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> AzureLoginCallback(string returnUrl = "/")
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if(!string.IsNullOrEmpty(info.AuthenticationTokens.FirstOrDefault(a => a.Name.Equals("id_token"))?.Value))
                HttpContext.Session.SetString("id_token", info.AuthenticationTokens.FirstOrDefault(a => a.Name.Equals("id_token"))?.Value);

            var email = info.Principal.FindFirstValue(ClaimTypes.Upn) ?? string.Empty;
            var user = await userService.GetUserByEmail(email);
            if (user.IsNone)
            {
                if (info.LoginProvider.Equals(AzureADDefaults.AuthenticationScheme))
                {
                    ViewData["ReturnUrl"] = returnUrl;
                    TempData["RedirectErrorMessage"] = "Unauthorized account access";
                    var url = await LogOutAzureUser();
                    await auditService.CreateLogMessage(email, AuditLogActionType.Logout, DateTime.UtcNow);
                    return Redirect(url);
                }
                return Redirect(returnUrl);
            }
            else
            {
                if (string.IsNullOrEmpty(user.Get().Logins?.FirstOrDefault()?.ProviderKey))
                {
                    await userService.UpdateSecurityStampInternal(user.Get());
                    user.Get().Logins.FirstOrDefault()!.ProviderKey = info.ProviderKey;
                    await userService.UpdateUser(user.Get());
                }
            }
            
            var signInResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);
            if(signInResult.Succeeded)
            {
                await auditService.CreateLogMessage(email, AuditLogActionType.Login, DateTime.UtcNow);
                return RedirectToLocal(returnUrl);
            }

            return RedirectToAction(nameof(Login));
        }

        #region Helpers

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return Redirect("/");
            }
        }
        
        private void ClearUserData()
        {
            HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
            foreach (var cookie in HttpContext.Request.Cookies)
            {
                Response.Cookies.Delete(cookie.Key);
            }
        }

        private async Task<string> LogOutAzureUser()
        {
            var idTokenUrlParam = $"&id_token_hint={HttpContext.Session.GetString("id_token")}";
            var postLogoutRedirectUri =  $"post_logout_redirect_uri={Request.Scheme}://{Request.Host}{azureAdSetting.SignedOutCallbackPath}";
            var url = $"{azureAdSetting.RemoteSignOutPath}?{postLogoutRedirectUri}/{idTokenUrlParam}";

            ClearUserData();
            
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            await HttpContext.SignOutAsync(AzureADDefaults.AuthenticationScheme);

            return url;
        }

        private async Task<ApiResult<UpdateState>> InitialAdminAccount()
        {
            var policy = new RolePolicy[]
            {
                RolePolicy.Home, RolePolicy.Upload, RolePolicy.PriceEventsLog, RolePolicy.SystemLog,
                RolePolicy.AuditLog, RolePolicy.RegisterRole, RolePolicy.RegisterUser, RolePolicy.Version
            };
            return await userRoleService.UpdateUserRole(new UserRole {Policy = policy, Role = "Admin"});
        }
        #endregion
    }
}
