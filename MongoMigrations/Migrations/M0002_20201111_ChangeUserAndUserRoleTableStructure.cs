using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.Identity.Mongo.Mongo;
using CTO.Price.Admin.Data;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Identity;
using Mongo.Migration.Migrations.Database;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Price.MongoMigrations
{
    public class M0002_20201111_ChangeUserAndUserRoleTableStructure : DatabaseMigration
    {
        public M0002_20201111_ChangeUserAndUserRoleTableStructure() : base("1.0.1") { }
        private const string UserRoleCollectionName = "UserRole";
        private const string UserCollectionName = "User";
        
        private const string IdBsonElementName = "_id";
        private const string RoleBsonElementName = "Role";
        private const string PolicyBsonElementName = "Policy";
        private const string TempBsonElementName = "IsOriginal";
        private const string EmailBsonElementName = "Email";
        private const string RolesBsonElementName = "Roles";
        private const string LastUpdateBsonElementName = "LastUpdate";
        private readonly BsonArray defaultPolicyBsonElementValue = new BsonArray() {"Home","Upload","PriceEventsLog","AuditLog","RegisterUser","RegisterRole","Version","SystemLog"};
        private readonly string defaultRoleBsonElementValue = "Admin";
        
        public override void Up(IMongoDatabase db)
        {
            MigrationUpUserRole(db).Wait();
            MigrationUpUser(db);
        }

        public override void Down(IMongoDatabase db)
        {
            MigrationDownUser(db);
            MigrationDownUserRole(db);
        }

        #region Private method
        private async Task MigrationUpUserRole(IMongoDatabase db)
        {
            var collection = db.GetCollection<BsonDocument>(UserRoleCollectionName);
            var inserted = collection.Find(x => true).ToList();

            if (inserted?.Count > 0)
            {
                // add new empty field into collection 
                foreach (var item in inserted)
                {
                    item.Add(new BsonElement(RoleBsonElementName, string.Empty));
                    collection.ReplaceOne(new BsonDocument(IdBsonElementName, item[IdBsonElementName]), item);
                }

                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq(RoleBsonElementName, string.Empty);
                var list = collection.Find(filter).ToList();

                var allCopy = list.Select(d => new BsonDocument
                {
                    {IdBsonElementName, ObjectId.GenerateNewId()}, {RoleBsonElementName, d[IdBsonElementName]},
                    {PolicyBsonElementName, d[PolicyBsonElementName]}
                }).ToList();

                if (allCopy.Any())
                    collection.InsertMany(allCopy);

                collection.DeleteMany(filter);
            }
            else
            {
                await collection.InsertOneAsync(new BsonDocument
                {
                    {IdBsonElementName, ObjectId.GenerateNewId()},
                    {RoleBsonElementName, defaultRoleBsonElementValue},
                    {PolicyBsonElementName, defaultPolicyBsonElementValue}
                });
            }
        }
        
        private void MigrationDownUserRole(IMongoDatabase db)
        {
            var collection = db.GetCollection<BsonDocument>(UserRoleCollectionName);
            var inserted = collection.Find(x => true).ToList();
            foreach (var item in inserted)
            {
                item.Add(new BsonElement(TempBsonElementName, true));
                collection.ReplaceOne(new BsonDocument(IdBsonElementName, item[IdBsonElementName]), item);
            }
            
            var allCopy = inserted.Select(d => new BsonDocument
            {
                { IdBsonElementName, d[RoleBsonElementName] }, { PolicyBsonElementName, d[PolicyBsonElementName]}
            }).ToList();

            if (allCopy.Any())
                collection.InsertMany(allCopy);
            
            var filter = Builders<BsonDocument>.Filter.Eq(TempBsonElementName, BsonBoolean.True);
            collection.DeleteMany(filter);
        }
        
        private void MigrationDownUser(IMongoDatabase db)
        {
            var collection = db.GetCollection<BsonDocument>(UserCollectionName);
            var all = collection.Find(_ => true).ToListAsync().Result;
            
            foreach (var item in all)
            {
                item.Add(new BsonElement(TempBsonElementName, true));
                collection.ReplaceOne(new BsonDocument(IdBsonElementName, item[IdBsonElementName]), item);
            }

            var allCopy = all.Select(d => new BsonDocument
            {
                { IdBsonElementName, d[EmailBsonElementName] }, { RoleBsonElementName, GetRoleNameById(db, d[RolesBsonElementName].ToString())} , { LastUpdateBsonElementName, DateTime.UtcNow }
            }).ToList();
            
            if (allCopy.Any())
                collection.InsertMany(allCopy);
            
            var filter = Builders<BsonDocument>.Filter.Eq(TempBsonElementName, BsonBoolean.True);
            collection.DeleteMany(filter);
        }
        
        private void MigrationUpUser(IMongoDatabase db)
        {
            var collection = db.GetCollection<BsonDocument>(UserCollectionName);
            var all = collection.Find(_ => true).ToListAsync().Result;
            
            foreach (var item in all)
            {
                item.Add(new BsonElement(TempBsonElementName, true));
                collection.ReplaceOne(new BsonDocument(IdBsonElementName, item[IdBsonElementName]), item);
            }
            
            var allCopy = all.Select(d => new ApplicationUser
            {
                UserName = d[IdBsonElementName]!.ToString()!.Substring(0, d[IdBsonElementName]!.ToString()!.IndexOf('@')),
                NormalizedUserName = d[IdBsonElementName]!.ToString()!.Substring(0, d[IdBsonElementName]!.ToString()!.IndexOf('@')).ToUpper(),
                Email = d[IdBsonElementName].ToString(),
                NormalizedEmail = d[IdBsonElementName]!.ToString()!.ToUpper(),
                Logins = new List<IdentityUserLogin<string>>
                {
                    new IdentityUserLogin<string>
                    {
                        LoginProvider = AzureADDefaults.AuthenticationScheme,
                        ProviderDisplayName = AzureADDefaults.DisplayName
                    }
                }, 
                Roles = new List<string>{ GetRoleIdByName(db, d[RoleBsonElementName].ToString()) }
            }).ToArray();
            
            var applicationUserCollection = db.GetCollection<ApplicationUser>(UserCollectionName);
            if (allCopy.Any())
                applicationUserCollection.InsertMany(allCopy);
            
            var filter = Builders<BsonDocument>.Filter.Eq(TempBsonElementName, BsonBoolean.True);
            collection.DeleteMany(filter);
        }
        
        private string GetRoleIdByName(IMongoDatabase db, string name)
        {
            var collection = db.GetCollection<UserRole>(UserRoleCollectionName);
            return collection.FirstOrDefaultAsync(x => x.Role.Equals(name)).Result.Id;
        }
        
        private string GetRoleNameById(IMongoDatabase db, string idArray)
        {
            var ids = idArray.TrimStart('[').TrimEnd(']').Split(',');
            var roleId = ids.Any() ? ids.First() : null; 
            var collection = db.GetCollection<UserRole>(UserRoleCollectionName);
            return roleId != null ? collection.FirstOrDefaultAsync(x => x.Id.Equals(roleId)).Result.Role : string.Empty;
        }
        #endregion
    }
}