using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTO.Price.Admin.Data;
using CTO.Price.Shared;
using static RZ.Foundation.Prelude;

namespace CTO.Price.Admin.Services
{
    public interface IAuditService
    {
        Task CreateLogMessage(string email, AuditLogActionType actionType, DateTime logTime);
        Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate, AuditLogActionType actionType, string email);
        Task<List<AuditLog>> GetAuditLogWithFilter(DateTime fromDate, DateTime endDate, AuditLogActionType actionType, string email, int pageIndex, int pageSize);
        Task CreateLogMessage(string email, AuditLogActionType actionType, string result, DateTime logTime);
    }
    
    public class AuditService : IAuditService
    {
        readonly IAuditStorage auditStorage;

        public AuditService(IAuditStorage auditStorage) {
            this.auditStorage = auditStorage;
        }

        public async Task CreateLogMessage(string email, AuditLogActionType actionType, DateTime logTime) {
            await CreateLogMessage(email, actionType, string.Empty, logTime);
        }

        public async Task CreateLogMessage(string email, AuditLogActionType actionType, string result, DateTime logTime) {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                Email = email,
                Action = actionType,
                Result = result,
                LogTime = logTime
            };

            await TryAsync(() => DocumentHelper.TryAddNew(() => auditStorage.NewDocument(log))).Try();
        }
        
        public async Task<long> GetTotalRecordWithFilter(DateTime fromDate, DateTime endDate, AuditLogActionType actionType, string email)
            => await auditStorage.GetTotalRecordWithFilter(fromDate, endDate, actionType, email);
        public async Task<List<AuditLog>> GetAuditLogWithFilter(DateTime fromDate, DateTime endDate, AuditLogActionType actionType, string email, int pageIndex, int pageSize)
            => await auditStorage.GetAuditLogWithFilter(fromDate, endDate, actionType, email, pageIndex, pageSize);
    }
}