using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using CTO.Price.Scheduler.Actors;
using CTO.Price.Scheduler.Services;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Log;
using CTO.Price.Shared.Middleware;
using CTO.Price.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;


namespace CTO.Price.Scheduler
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.With<CustomLogLevel>()
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
                // .WriteTo.MongoDB("mongodb://mymongodb/","logs", LogEventLevel.Warning)
                .CreateLogger();

            services.AddLogging(a => a.AddSerilog(Log.Logger));

            services.AddControllers();
            services.AddHealthChecks().AddCheck<HealthCheck>("Health Check");

            services.Configure<PriceDefaults>(Configuration.GetSection("Defaults"));
            services.Configure<KafkaBusOption>(Configuration.GetSection("Kafka"));
            services.Configure<MessageBusOption>(Configuration.GetSection("MessageBus"));
            services.Configure<PublishConfiguration>(Configuration.GetSection("Publish"));
            services.Configure<PriceStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<DeleteSkuStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<ScheduleStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<EventLogStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<SystemLogStorageOption>(Configuration.GetSection("Storage"));
            
            services.AddTransient<NodeManager>();
            services.AddSingleton<INodeManagerProxy, NodeManagerProxy>();

            services.AddSingleton<IMessageBus, MessageBus>();
            services.AddSingleton(p => p.GetService<IMessageBus>().CreatePublisher());
            services.AddSingleton<IPerformanceCounter, PerformanceCounter>();
            
            services.AddSingleton<IPriceStorage, PriceStorage>();
            services.AddSingleton<IDeleteSkuStorage, DeleteSkuStorage>();
            services.AddSingleton<IPriceService, PriceService>();
            services.AddSingleton<IScheduleStorage, ScheduleStorage>();
            services.AddSingleton<IScheduleService, ScheduleService>();
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddSingleton<IEventLogService, EventLogService>();
            services.AddSingleton<IEventLogStorage, EventLogStorage>();
            
            services.AddSingleton<ISystemLogService, SystemLogService>();
            services.AddSingleton<ISystemLogStorage, SystemLogStorage>();
            
            services.SetupActorEngine<ActorEngineStartup>(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            if (env.IsEnvironment("local") || env.IsEnvironment("dev"))
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseMiddleware<ExceptionMiddleware>();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });

            app.StartActorEngine<ActorEngineStartup>(lifetime);
        }
    }
}