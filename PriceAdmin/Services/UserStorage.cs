using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Shared;
using CTO.Price.Shared.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RZ.Foundation;

namespace CTO.Price.Admin.Services
{
    [ExcludeFromCodeCoverageAttribute]
    public sealed class UserStorageOption
    {
        public string ConnectionString { get; set; } = string.Empty;
    }
    public interface IUserStorage : IStorage<ApplicationUser>
    {
        Task<List<ApplicationUser>> GetAllUser();
        Task<Option<ApplicationUser>> GetUserByEmail(string email);
        Task<Option<ApplicationUser>> GetUserByName(string name);
        Task<ApiResult<ApplicationUser>> DeleteUser(ApplicationUser user);
        Task<ApiResult<ApplicationUser>> UpdateUser(ApplicationUser user);
    }
    
    public class UserStorage : IUserStorage
    {
        readonly IMongoCollection<ApplicationUser> userTable;

        [ExcludeFromCodeCoverageAttribute]
        public UserStorage(IOptionsMonitor<UserStorageOption> storageOption) {
            var databaseName = MongoUrl.Create(storageOption.CurrentValue.ConnectionString).DatabaseName;
            var client = new MongoClient(storageOption.CurrentValue.ConnectionString);
            var db = client.GetDatabase(databaseName);
            userTable = db.GetCollection<ApplicationUser>("User");
        }

        public UserStorage(IMongoCollection<ApplicationUser> mongoCollection) {
            userTable = mongoCollection;
        }

        public async Task<List<ApplicationUser>> GetAllUser() => await userTable.Find(_ => true).ToListAsync();

        public async Task<Option<ApplicationUser>> GetUserByEmail(string email) =>
            await (await userTable.FindAsync(u => u.Email == email)).SingleOrDefaultAsync();
        
        public async Task<Option<ApplicationUser>> GetUserByName(string name) =>
            await (await userTable.FindAsync(u => u.UserName == name)).SingleOrDefaultAsync();

        public async Task<ApiResult<ApplicationUser>> DeleteUser(ApplicationUser user) {
            var result = await userTable.DeleteOneAsync(u => u.Email == user.Email);
            return result.DeletedCount == 1
                ? user
                : new PriceServiceException(PriceErrorCategory.DeleteFailed, $"user {user.Email} delete failed.").AsApiFailure<ApplicationUser>();
        }

        public async Task<ApiResult<ApplicationUser>> UpdateUser(ApplicationUser user) {
            var result = await userTable.ReplaceOneAsync(u => u.Email == user.Email, user,
                new ReplaceOptions {IsUpsert = true});
            return result.ModifiedCount == 1
                ? user
                : new PriceServiceException(PriceErrorCategory.UpdateFailed, $"user {user.Email} update failed.").AsApiFailure<ApplicationUser>();
        }
        
        public async Task<ApplicationUser> NewDocument(ApplicationUser document) => await DocumentHelper.NewMongoDocument(userTable, document);
        public async Task NewDocuments(IEnumerable<ApplicationUser> documents) => await DocumentHelper.NewMongoDocuments(userTable, documents);

        public async Task<ApplicationUser> UpdateDocument(ApplicationUser document, Expression<Func<ApplicationUser, bool>> filter) 
            => await DocumentHelper.UpdateMongoDocument(userTable, document, filter);

        public async Task<ApplicationUser> DeleteDocument(string identifier, Expression<Func<ApplicationUser, bool>> filter) 
            => await DocumentHelper.DeleteMongoDocument(userTable, identifier, filter);

        public async Task<long> DeleteDocuments(string identifier, Expression<Func<ApplicationUser, bool>> filter)
            => await DocumentHelper.DeleteMongoDocuments(userTable, identifier, filter);
    }
}