using System.Collections.Generic;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Shared;
using RZ.Foundation;
using static RZ.Foundation.Prelude;

namespace CTO.Price.Admin.Services
{
    public interface IUserRoleService
    {
        Task<List<UserRole>> GetAllUserRole();
        Task<Option<UserRole>> GetUserRole(string role);
        Task<Option<UserRole>> GetUserRoleById(string roleId);
        Task<ApiResult<UpdateState>> UpdateUserRole(UserRole role);
        Task<ApiResult<UpdateState>> DeleteUserRole(UserRole role);
    }
    
    public class UserRoleService : IUserRoleService
    {
        readonly IUserRoleStorage userRoleStorage;

        public UserRoleService(IUserRoleStorage userRoleStorage) {
            this.userRoleStorage = userRoleStorage;
        }

        public async Task<List<UserRole>> GetAllUserRole() => await userRoleStorage.GetAllUserRole();
        
        public async Task<Option<UserRole>> GetUserRole(string role) => await userRoleStorage.GetUserRole(role);

        public async Task<Option<UserRole>> GetUserRoleById(string roleId) => await userRoleStorage.GetUserRoleById(roleId);

        public async Task<ApiResult<UpdateState>> UpdateUserRole(UserRole role) {
            var roleDb = await userRoleStorage.GetUserRole(role.Role);

            return await TryAsync(() => roleDb.Get(update => DocumentHelper.TryUpdate(() =>
                    userRoleStorage.UpdateDocument(role, r => r.Role == role.Role)),
                () => DocumentHelper.TryAddNew(() => userRoleStorage.NewDocument(role)))).Try();
        }

        public async Task<ApiResult<UpdateState>> DeleteUserRole(UserRole role) {
            var roleDb = await userRoleStorage.GetUserRole(role.Role);

            return await TryAsync(() => roleDb.Get(delete => DocumentHelper.TryDelete(() =>
                    userRoleStorage.DeleteDocument(delete.Role, r => r.Role == role.Role)),
                () => Task.FromResult(UpdateState.Ignore))).Try();
        }
    }
}