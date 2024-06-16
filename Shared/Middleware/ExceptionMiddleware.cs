using System;
using System.Threading.Tasks;
using CTO.Price.Shared.Services;
using Microsoft.AspNetCore.Http;

namespace CTO.Price.Shared.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ISystemLogService systemLogService;

        public ExceptionMiddleware(RequestDelegate next, ISystemLogService systemLogService)
        {
            this._next = next;
            this.systemLogService = systemLogService;
        }
        
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await systemLogService.Error(ex, ex.Message);
            }
        }
    }
}