using System;
using System.Diagnostics.CodeAnalysis;
using Amazon;
using Amazon.S3;
using CTO.Price.Api.Interceptors;
using CTO.Price.Api.Services;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Log;
using CTO.Price.Shared.Services;
using Elasticsearch.Net;
using Elasticsearch.Net.Aws;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nest;
using Serilog;
using StackExchange.Redis;

namespace CTO.Price.Api
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private readonly IWebHostEnvironment env;

        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            this.env = env;
            Configuration = configuration;
        }

        IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();

            services.Configure<AwsSettings>(Configuration.GetSection("AWS"));
            services.Configure<PriceDefaults>(Configuration.GetSection("Defaults"));
            services.Configure<KafkaBusOption>(Configuration.GetSection("Kafka"));
            services.Configure<MessageBusOption>(Configuration.GetSection("MessageBus"));
            services.Configure<PriceStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<DeleteSkuStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<ScheduleStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<EventLogStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<SystemLogStorageOption>(Configuration.GetSection("Storage"));

            services.AddTransient<NodeManager>();
            services.AddSingleton<INodeManagerProxy, NodeManagerProxy>();
            
            services.AddSingleton<IMessageBus, MessageBus>();
            services.AddSingleton(p => p.GetService<IMessageBus>().CreatePublisher());
            
            services.AddSingleton<IPriceStorage, PriceStorage>();
            services.AddSingleton<IDeleteSkuStorage, DeleteSkuStorage>();
            services.AddSingleton<IPriceService, PriceService>();
            services.AddSingleton<IScheduleStorage, ScheduleStorage>();
            services.AddSingleton<IScheduleService, ScheduleService>();
            services.AddSingleton<IPerformanceCounter, PerformanceCounter>();
            services.AddSingleton((ILogger)new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.With<CustomLogLevel>()
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
                .CreateLogger());
            services.AddSingleton<IEventLogService, EventLogService>();

            services.AddSingleton<IEventLogStorage, EventLogStorage>();
            services.AddSingleton<ISystemLogService, SystemLogService>();
            services.AddSingleton<ISystemLogStorage, SystemLogStorage>();

            if (env.IsEnvironment("local")) {
                services.AddSingleton<IAmazonS3>(provider => {
                    var settings = provider.GetService<IOptions<AwsSettings>>().Value;
                    return new AmazonS3Client(new AmazonS3Config {
                        RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region),
                        UseHttp = true,
                        ServiceURL = settings.S3.ServiceUrl,
                        ForcePathStyle = true
                    });
                });
                services.AddSingleton<IElasticClient>(provider =>
                {
                    var settings = provider.GetService<IOptions<AwsSettings>>().Value;
                    var pool = new SingleNodeConnectionPool(new Uri(settings.Es.Uri));
                    var config = new ConnectionSettings(pool);
                    return new ElasticClient(config);
                });
            } else {
                services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
                services.AddAWSService<IAmazonS3>();
                services.AddSingleton<IElasticClient>(provider =>
                {
                    var httpConnection = new AwsHttpConnection();
                    var settings = provider.GetService<IOptions<AwsSettings>>().Value;
                    var pool = new SingleNodeConnectionPool(new Uri(settings.Es.Uri));
                    var config = new ConnectionSettings(pool, httpConnection);
                    return new ElasticClient(config);
                });
            }

            services.AddSingleton<IFileStorageService, FileStorageService>();
            
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<LoggerInterceptor>();
            });

            services.AddControllers();
            
            // services.SetupActorEngine<ActorEngineStartup>(Configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            if (env.IsEnvironment("local") || env.IsEnvironment("dev")){
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                
                endpoints.MapGrpcService<SystemInfoService>();
                endpoints.MapGrpcService<PriceApiServiceV1>();
                endpoints.MapGrpcService<PriceApiServiceV2>();
                endpoints.MapGrpcService<PerformanceService>();
                
                endpoints.MapHealthChecks("/health");

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
            
            // app.StartActorEngine<ActorEngineStartup>(lifetime);
        }
    }
}