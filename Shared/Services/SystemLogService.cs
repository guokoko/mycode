using System;
using System.Threading.Tasks;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Extensions;
using CTO.Price.Shared.Log;
using Microsoft.Extensions.Logging;


namespace CTO.Price.Shared.Services
{
    public interface ISystemLogService
    {
        void Debug(string message);
        Task Warning(string message);
        Task Error(Exception exception, string message);
        void Info(string message);
    }

    public class SystemLogService : ISystemLogService
    {
        readonly ILogger<SystemLogService> logger;
        readonly string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!;
        readonly ISystemLogStorage systemLogStorage;
        public SystemLogService(ILogger<SystemLogService> logger, ISystemLogStorage systemLogStorage)
        {
            this.logger = logger;
            this.systemLogStorage = systemLogStorage;
        }
        
        #region Debug Logger
        public void Debug(string message)
        {
            var systemLog = new SystemLog {Message = message};
            logger.LogDebug(systemLog.Message.ToJsonString());
        }

        public async Task Warning(string message)
        {
            logger.LogWarning(message);
            
            var systemLog = new SystemLog
            {
                Level = LogLevelEnum.warning,
                Environment = environment,
                Message = message
            };
            await systemLogStorage.NewDocument(systemLog);
        }

        #endregion

        #region Error Logger
        public async Task Error(Exception exception, string message)
        {
            logger.LogError(exception, exception.Message);
            
            var systemLog = new SystemLog
            {
                Level = LogLevelEnum.error,
                Environment = environment,
                ProjectName = exception.Source,
                Message = exception.Message,
                MessageDetail = exception.StackTrace
            };
            await systemLogStorage.NewDocument(systemLog);
        }
        #endregion

        public void Info(string message)
        {   
            var systemLog = new SystemLog
            {
                Level = LogLevelEnum.info,
                Environment = environment,
                Message = message
            };
            logger.LogInformation(systemLog.Message);
        }
    }
}