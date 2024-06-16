using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Akka.Util;
using CTO.Price.Shared.Domain;
using MongoDB.Driver;
using Newtonsoft.Json;
using RZ.Foundation;
using RZ.Foundation.Types;
using static RZ.Foundation.Prelude;

namespace CTO.Price.Shared
{
    public static class DocumentHelper
    {
        public static async Task<UpdateState> TryAddNew<T>(Func<Task<T>> create) =>
            (await TryAsync(create).Try()).Get(_ => UpdateState.Updated, exception => exception switch
            {
                PriceServiceException ex => throw ex,
                _ => throw exception
            });

        public static async Task<UpdateState> TryUpdate<T>(Func<Task<T>> update) =>
            (await TryAsync(update).Try()).Get(_ => UpdateState.Updated, exception => exception switch
            {
                PriceServiceException ex => throw ex,
                _ => throw exception
            });

        public static async Task<UpdateState> TryDelete<T>(Func<Task<T>> delete, UpdateState @default = UpdateState.NeedRetry) =>
            (await TryAsync(delete).Try()).Get(_ => UpdateState.Deleted, exception => exception switch
            {
                PriceServiceException ex => throw ex,
                _ => throw exception
            });

        public static async Task<T> NewMongoDocument<T>(IMongoCollection<T> collection, T document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            try {
                await collection.InsertOneAsync(document);
                return document;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        public static async Task NewMongoDocuments<T>(IMongoCollection<T> collection, IEnumerable<T> documents)
        {
            if (!documents.Any())
                return;
            try {
                await collection.InsertManyAsync(documents);
                return;
            }
            catch (Exception ex)
            {
                throw new PriceServiceException(PriceErrorCategory.UnexpectedError, $"Unexpected exception! {ex.Message}", cause: ex);
            }
        }
        
        public static async Task<T> UpdateMongoDocument<T>(IMongoCollection<T> collection, T document, Expression<Func<T, bool>> filter) {
            try
            {
                var options = new FindOneAndReplaceOptions<T>()
                {
                    IsUpsert = false,
                    ReturnDocument = ReturnDocument.After
                };
                var result = await collection.FindOneAndReplaceAsync(filter, document, options);
                return result ?? throw new PriceServiceException(PriceErrorCategory.UpdateFailed, $"MongoDb {typeof(T)} {document} update failed.");
            }
            catch (Exception ex)
            {
                if (ex is PriceServiceException pse)
                    throw pse;
                throw new PriceServiceException(PriceErrorCategory.UnexpectedError, $"Unexpected exception! {ex.Message}", cause: ex);
            }
        }

        public static async Task<T> DeleteMongoDocument<T>(IMongoCollection<T> collection, string identifier, Expression<Func<T, bool>> filter)
        {
            try
            {
                var result = await collection.FindOneAndDeleteAsync(filter);
                return result ?? throw new PriceServiceException(PriceErrorCategory.UpdateFailed, $"MongoDb {typeof(T)} {identifier} delete failed.");
            }
            catch (Exception ex)
            {
                if (ex is PriceServiceException pse)
                    throw pse;
                throw new PriceServiceException(PriceErrorCategory.UnexpectedError, $"Unexpected exception! {ex.Message}", cause: ex);
            }
        }
        
        public static async Task<long> DeleteMongoDocuments<T>(IMongoCollection<T> collection, string identifier, Expression<Func<T, bool>> filter)
        {
            try
            {
                var result = await collection.DeleteManyAsync(filter);
                return result.DeletedCount;
            }
            catch (Exception ex)
            {
                if (ex is PriceServiceException pse)
                    throw pse;
                throw new PriceServiceException(PriceErrorCategory.UnexpectedError, $"Unexpected exception! {ex.Message}", cause: ex);
            }
        }

        public static async Task<UpdateState> TryUpdateThrow<T>(Func<Task<T>> update)
        {
            try
            {
                await update();
                return UpdateState.Updated;
            }
            catch
            {
                throw;
            }
        }

        public static async Task<T> UpdateMongoDocumentThrow<T>(IMongoCollection<T> collection, T document, Expression<Func<T, bool>> filter)
        {
            try
            {
                var options = new FindOneAndReplaceOptions<T>
                {
                    IsUpsert = false,
                    ReturnDocument = ReturnDocument.After
                };
                var result = await collection.FindOneAndReplaceAsync(filter, document, options);
                return result;
            }
            catch
            {
                throw;
            }
        }

        public static async Task<T> NewMongoDocumentReplace<T>(IMongoCollection<T> collection, T document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            try
            {
                var keyProperty = typeof(T).GetProperty("Key") ?? throw new ArgumentException("Document must have a 'Key' property.");
                var key = keyProperty.GetValue(document) ?? throw new ArgumentException("Document 'Key' property cannot be null.");
                var filter = Builders<T>.Filter.Eq("Key", key);
                var options = new ReplaceOptions { IsUpsert = true };

                await collection.ReplaceOneAsync(filter, document, options);
                return document;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

        public enum UpdateState
    {
        Updated,
        NeedRetry,
        Ignore,
        Deleted,
        DuplicateKey
    }
    
    public enum UpdateResult
    {
        Created, 
        Updated, 
        Ignored,
        Deleted
    }

    public interface IStorage<T>
    {
        Task<T> NewDocument(T document);
        Task NewDocuments(IEnumerable<T> documents);
        Task<T> UpdateDocument(T document, Expression<Func<T, bool>> filter);
        Task<T> DeleteDocument(string identifier, Expression<Func<T, bool>> filter);
        Task<long> DeleteDocuments(string identifier, Expression<Func<T, bool>> filter);
    }

}