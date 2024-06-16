using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Shared;
using Microsoft.AspNetCore.Identity;
using RZ.Foundation;
using static RZ.Foundation.Prelude;

namespace CTO.Price.Admin.Services
{
    public interface IUserService
    {
        Task<List<ApplicationUser>> GetAllUser();
        Task<List<UserRoleViewModel>> GetAllUserRoleDetail();
        Task<Option<ApplicationUser>> GetUserByEmail(string email);
        Task<Option<ApplicationUser>> GetUserByName(string name);
        Task<List<UserRoleViewModel>> GetUserByEmailRoleProvider(string email, string role, string employeeType, int pageIndex, int pageSize);
        Task<long> GetTotalRecordWithFilter(string email, string role, string employeeType);
        Task<ApiResult<UpdateState>> DeleteUser(ApplicationUser user);
        Task<ApiResult<UpdateState>> UpdateUser(ApplicationUser user);
        Task<IdentityResult> CreateApplicationUserWithPassword(ApplicationUser user, string password);
        Task UpdateSecurityStampInternal(ApplicationUser user);
    }
    
    public class UserService : IUserService
    {
        readonly IUserStorage userStorage;
        readonly IUserRoleStorage userRoleStorage;
        readonly UserManager<ApplicationUser> userManager;

        public UserService(IUserStorage userStorage, IUserRoleStorage userRoleStorage, UserManager<ApplicationUser> userManager) {
            this.userStorage = userStorage;
            this.userRoleStorage = userRoleStorage;
            this.userManager = userManager;
        }
        
        

        public async Task<List<ApplicationUser>> GetAllUser() => await userStorage.GetAllUser();

        public async Task<List<UserRoleViewModel>> GetAllUserRoleDetail()
        {
            var userRoles = new List<UserRoleViewModel>();
            var users = await GetAllUser();
            if (users.Count > 0)
            {
                foreach (var user in users)
                {
                    var roleId = user.Roles.First();
                    var role = await userRoleStorage.GetUserRoleById(roleId);
                    if (role.IsSome)
                    {
                        userRoles.Add(new UserRoleViewModel
                        {
                            User = user,
                            UserRole = role.Get()
                        });
                    }
                }
            }

            return userRoles;
        }

        public async Task<Option<ApplicationUser>> GetUserByEmail(string email) {
            return await userStorage.GetUserByEmail(email);
        }
        
        public async Task<Option<ApplicationUser>> GetUserByName(string name) {
            return await userStorage.GetUserByName(name);
        }

        public async Task<List<UserRoleViewModel>> GetUserByEmailRoleProvider(string email, string role,
            string employeeType, int pageIndex, int pageSize) => (await GetAllUserRoleDetail()).FindAll(x
                            => (string.IsNullOrEmpty(email) || x.User.NormalizedEmail.Contains(email.ToUpper()))
                               && (string.IsNullOrEmpty(employeeType) || x.User.Logins.Any(y => y.LoginProvider.Equals(employeeType)))
                               && (string.IsNullOrEmpty(role) || x.UserRole.Role.Equals(role))).OrderByDescending(o => o.User.Email).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

        public async Task<long> GetTotalRecordWithFilter(string email, string role, string employeeType)
            => (await GetAllUserRoleDetail()).Count(x =>
                (string.IsNullOrEmpty(email) || x.User.NormalizedEmail.Contains(email.ToUpper()))
                && (string.IsNullOrEmpty(employeeType) || x.User.Logins.Any(y => y.LoginProvider.Equals(employeeType)))
                && (string.IsNullOrEmpty(role) || x.UserRole.Role.Equals(role)));
        
        
        public async Task<ApiResult<UpdateState>> DeleteUser(ApplicationUser user) {
            var userDb = await userStorage.GetUserByEmail(user.Email);
            
            return await TryAsync(() => userDb.Get(delete => DocumentHelper.TryDelete(() =>
                userStorage.DeleteDocument(delete.Email,
                    u => u.Email == delete.Email)), () => Task.FromResult(UpdateState.Ignore))).Try();
        }

        public async Task<ApiResult<UpdateState>> UpdateUser(ApplicationUser user) {
            user.Email = user.Email.ToLower();
            var userDb = await userStorage.GetUserByEmail(user.Email);

            return await TryAsync(() => userDb.Get(update => DocumentHelper.TryUpdate(() =>
                    userStorage.UpdateDocument(user, u => u.Email == user.Email)),
                () => DocumentHelper.TryAddNew(() => userStorage.NewDocument(user)))).Try();
        }
        public async Task<IdentityResult> CreateApplicationUserWithPassword(ApplicationUser user, string password) => await userManager.CreateAsync(user, password);
        public async Task UpdateSecurityStampInternal(ApplicationUser user) => await userManager.UpdateSecurityStampAsync(user);
    }
}