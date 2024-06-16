using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CTO.Price.Shared.Services;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace CTO.Price.Api.Interceptors
{
    [ExcludeFromCodeCoverage]
    public class LoggerInterceptor : Interceptor
    {
        private readonly ISystemLogService systemLogService;

        public LoggerInterceptor(ISystemLogService systemLogService)
        {
            this.systemLogService = systemLogService;
        }

        public async override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await continuation(request, context);
            }
            catch (Exception e)
            {
                await systemLogService.Error(e, e.Message);
                throw;
            }
        }
    }
}