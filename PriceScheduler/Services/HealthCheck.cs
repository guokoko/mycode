using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTO.Price.Scheduler.Actors;
using CTO.Price.Shared.Log;
using CTO.Price.Shared.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

namespace CTO.Price.Scheduler.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class HealthCheck : IHealthCheck
    {
        private readonly IPerformanceCounter performanceRecorder;
        public HealthCheck(IPerformanceCounter performanceRecorder)
        {
            this.performanceRecorder = performanceRecorder;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {

            return Task.FromResult(HealthCheckResult.Healthy("A healthy result."));
        }
    }

}