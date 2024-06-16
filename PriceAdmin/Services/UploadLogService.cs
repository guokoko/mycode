using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Shared;
using static RZ.Foundation.Prelude;

namespace CTO.Price.Admin.Services
{
    public interface IUploadLogService
    {
        Task CreateLogMessage(string email, string fileName, UploadResult result);
        Task CreateLogMessage(string email, string fileName, UploadResult result, string detail);

        Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate, string fileName, UploadResult? result,
            string email);

        Task<List<UploadLog>> GetUploadLogWithFilter(DateTime fromDate, DateTime endDate, string fileName,
            UploadResult? result, string email, int pageIndex, int pageSize);
    }
    
    public class UploadLogService : IUploadLogService
    {
        readonly IUploadLogStorage uploadLogStorage;

        public UploadLogService(IUploadLogStorage uploadLogStorage)
        {
            this.uploadLogStorage = uploadLogStorage;
        }

        public async Task CreateLogMessage(string email, string fileName, UploadResult result)
            => await CreateLogMessage(email, fileName, result, string.Empty);

        public async Task CreateLogMessage(string email, string fileName, UploadResult result, string detail)
        {
            var log = new UploadLog
            {
                Id = Guid.NewGuid(),
                Email = email,
                FileName = fileName,
                Result = result,
                Detail = detail
            };

            await TryAsync(() => DocumentHelper.TryAddNew(() => uploadLogStorage.NewDocument(log))).Try();
        }

        public async Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate,
            string fileName, UploadResult? result, string email)
            => await uploadLogStorage.GetTotalRecordWithFilter(fromDate, endDate, fileName, result, email);

        public async Task<List<UploadLog>> GetUploadLogWithFilter(DateTime fromDate, DateTime endDate,
            string fileName, UploadResult? result, string email, int pageIndex, int pageSize)
            => await uploadLogStorage.GetUploadLogWithFilter(fromDate, endDate, fileName, result, email, pageIndex,
                pageSize);
    }
}