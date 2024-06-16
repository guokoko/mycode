using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Shared;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RZ.Foundation;

namespace CTO.Price.Admin.Services
{
    public interface IUserRoleStorage : IStorage<UserRole>
    {
        Task<List<UserRole>> GetAllUserRole();
        Task<Option<UserRole>> GetUserRole(string role);
        Task<Option<UserRole>> GetUserRoleById(string roleId);
    }
    
    public class UserRoleStorage : IUserRoleStorage
    {
        readonly IMongoCollection<UserRole> userRoleTable;

        [ExcludeFromCodeCoverageAttribute]
        public UserRoleStorage(IOptionsMonitor<UserStorageOption> storageOption) {
            var databaseName = MongoUrl.Create(storageOption.CurrentValue.ConnectionString).DatabaseName;
            var client = new MongoClient(storageOption.CurrentValue.ConnectionString);
            var db = client.GetDatabase(databaseName);
            userRoleTable = db.GetCollection<UserRole>("UserRole");
        }

        public UserRoleStorage(IMongoCollection<UserRole> mongoCollection) {
            userRoleTable = mongoCollection;
        }
        
        public async Task<List<UserRole>> GetAllUserRole() => await userRoleTable.Find(_ => true).ToListAsync();

        public async Task<Option<UserRole>> GetUserRole(string role) =>
            await (await userRoleTable.FindAsync(r => r.Role.ToLower() == role.ToLower())).SingleOrDefaultAsync();
        
        public async Task<Option<UserRole>> GetUserRoleById(string roleId) =>
            await (await userRoleTable.FindAsync(r => r.Id == roleId)).SingleOrDefaultAsync();

        public async Task<UserRole> NewDocument(UserRole document) => await DocumentHelper.NewMongoDocument(userRoleTable, document);
        public async Task NewDocuments(IEnumerable<UserRole> documents) => await DocumentHelper.NewMongoDocuments(userRoleTable, documents);

        public async Task<UserRole> UpdateDocument(UserRole document, Expression<Func<UserRole, bool>> filter) 
            => await DocumentHelper.UpdateMongoDocument(userRoleTable, document, filter);

        public async Task<UserRole> DeleteDocument(string identifier, Expression<Func<UserRole, bool>> filter) 
            => await DocumentHelper.DeleteMongoDocument(userRoleTable, identifier, filter);

        public async Task<long> DeleteDocuments(string identifier, Expression<Func<UserRole, bool>> filter)
            => await DocumentHelper.DeleteMongoDocuments(userRoleTable, identifier, filter);
    }
}