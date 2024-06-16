using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CTO.Price.Admin.Services;
using Microsoft.AspNetCore.Authorization;

namespace CTO.Price.Admin.Data
{
    public enum RolePolicy
    {
        Home,
        Upload,
        PriceEventsLog,
        AuditLog,
        RegisterUser,
        RegisterRole,
        Version,
        SystemLog
    }

    public class PageAuthorizedRequirement : IAuthorizationRequirement
    {
        public RolePolicy Policy { get; }

        public PageAuthorizedRequirement(RolePolicy policy) {
            Policy = policy;
        }
    }

    public class PageAuthorizedHandler : AuthorizationHandler<PageAuthorizedRequirement>
    {
        readonly IUserRoleService userRoleService;
        public PageAuthorizedHandler(IUserRoleService userRoleService) {
            this.userRoleService = userRoleService;
        }
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PageAuthorizedRequirement requirement) {
            if (!context.User.HasClaim(c => c.Type == ClaimTypes.Role)) {
                return;
            }
            
            var userRole = context.User.FindFirstValue(ClaimTypes.Role);
            var policyOption = await userRoleService.GetUserRole(userRole);
            if (policyOption.IsSome) {
                var policies = policyOption.Get().Policy;
                if (policies.Contains(requirement.Policy)) {
                    context.Succeed(requirement);
                    return;
                }
            }
            context.Fail();
        }
    }
}