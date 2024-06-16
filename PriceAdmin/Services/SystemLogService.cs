using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Log;

namespace CTO.Price.Admin.Services
{
    public interface ISystemLogService
    {
        Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate, string projectName, LogLevelEnum? level, string errorMessage);
        Task<List<SystemLog>> GetSystemLogWithFilter(DateTime fromDate, DateTime endDate, string projectName, LogLevelEnum? level,
            string errorMessage, int pageIndex, int pageSize);
    }
    
    public class SystemLogService : ISystemLogService
    {
        readonly ISystemLogStorage systemLogStorage;

        public SystemLogService(ISystemLogStorage systemLogStorage) {
            this.systemLogStorage = systemLogStorage;
        }

        public async Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate, string projectName, LogLevelEnum? level, string errorMessage)
            => await systemLogStorage.GetTotalRecordWithFilter(fromDate, endDate, projectName, level, errorMessage);
        public async Task<List<SystemLog>> GetSystemLogWithFilter(DateTime fromDate, DateTime endDate, string projectName, LogLevelEnum? level,
            string errorMessage, int pageIndex, int pageSize)
        => await systemLogStorage.GetSystemLogWithFilter(fromDate, endDate, projectName, level,
         errorMessage, pageIndex, pageSize);
    }
}