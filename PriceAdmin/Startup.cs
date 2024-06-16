using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using CTO.Price.Admin.Data;
using CTO.Price.Admin.Services;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Services;
using Elasticsearch.Net;
using Elasticsearch.Net.Aws;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nest;
using ISystemLogService = CTO.Price.Admin.Services.ISystemLogService;
using ISystemLogStorage = CTO.Price.Admin.Services.ISystemLogStorage;
using SystemLogStorageOption = CTO.Price.Admin.Services.SystemLogStorageOption;
using AspNetCore.Identity.Mongo;
using CTO.Price.Shared.Log;
using Serilog;

namespace CTO.Price.Admin
{
    public class Startup
    {
        private readonly IWebHostEnvironment env;
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            this.env = env;
            Configuration = configuration;
            StaticConfig = configuration;
        }

        public IConfiguration Configuration { get; }
        public static IConfiguration StaticConfig { get; private set; } = null!;


        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) {
            services.AddDistributedMemoryCache();
            
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            
            services.AddIdentityMongoDbProvider<ApplicationUser>(identity =>
                {
                    identity.Password.RequireDigit = true;
                    identity.Password.RequireLowercase = true;
                    identity.Password.RequireNonAlphanumeric = true;
                    identity.Password.RequireUppercase = true;
                    identity.Password.RequiredLength = 8;
                    identity.Password.RequiredUniqueChars = 1;
                } ,
                mongo =>
                {
                    mongo.ConnectionString = Configuration.GetValue<string>("Storage:ConnectionString");
                    mongo.UsersCollection = "User";
                }
            );
            
            services.AddHealthChecks();            
            
            services.Configure<AuditStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<AwsSettings>(Configuration.GetSection("AWS"));
            services.Configure<PaginationSetting>(Configuration.GetSection("PaginationSetting"));
            services.Configure<PriceEventLogStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<ServiceUris>(Configuration.GetSection("ServiceUris"));
            services.Configure<UploadLogStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<UserStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<SystemLogStorageOption>(Configuration.GetSection("Storage"));
            services.Configure<ConfigureMongoDbIndexesOption>(Configuration.GetSection("Storage"));
            services.Configure<LoggerRetentionDurationSetting>(Configuration.GetSection("LoggerRetentionDuration"));
            services.Configure<AzureAdSetting>(Configuration.GetSection("AzureAd"));
            services.AddSingleton<IAuditStorage, AuditStorage>();
            services.AddSingleton<IAuditService, AuditService>();
            services.AddSingleton<IPriceApi, PriceApi>();
            services.AddSingleton<IPriceEventLogService, PriceEventLogService>();
            services.AddSingleton<IPriceEventLogStorage, PriceEventLogStorage>();
            services.AddSingleton<IPriceScheduler, PriceScheduler>();
            services.AddSingleton<IUploadLogStorage, UploadLogStorage>();
            services.AddSingleton<IUploadLogService, UploadLogService>();
            services.AddSingleton<IUserStorage, UserStorage>(sp => new UserStorage(sp.GetService<IOptionsMonitor<UserStorageOption>>()));
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IUserRoleStorage, UserRoleStorage>();
            services.AddSingleton<IUserRoleService, UserRoleService>();
            services.AddSingleton<IAuthorizationHandler, PageAuthorizedHandler>();
            services.AddScoped<TimeZoneService>();
            services.AddSingleton<ISystemLogStorage, Services.SystemLogStorage>();
            services.AddSingleton<ISystemLogService, Services.SystemLogService>();
            services.AddScoped<SpinnerService>();
            services.AddHostedService<ConfigureMongoDbIndexesService>();
            services.AddSingleton((ILogger)new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.With<CustomLogLevel>()
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
                .CreateLogger());
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


            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                .AddAzureAD(op => Configuration.Bind("AzureAd", op));

            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, op =>
            {
                op.SaveTokens = true;
                op.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false
                };
                op.Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = context => {
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context => {
                        context.Response.Redirect("/Error");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddControllersWithViews(op => {
                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                op.Filters.Add(new AuthorizeFilter(policy));
            });

            services.AddAuthorization(config =>
            {
                config.AddPolicy( RolePolicy.Home.ToString(), 
                    policy => policy.Requirements.Add(new PageAuthorizedRequirement(RolePolicy.Home)));
                config.AddPolicy( RolePolicy.Upload.ToString(), 
                    policy => policy.Requirements.Add(new PageAuthorizedRequirement(RolePolicy.Upload)));
                config.AddPolicy( RolePolicy.PriceEventsLog.ToString(), 
                    policy => policy.Requirements.Add(new PageAuthorizedRequirement(RolePolicy.PriceEventsLog)));
                config.AddPolicy( RolePolicy.AuditLog.ToString(), 
                    policy => policy.Requirements.Add(new PageAuthorizedRequirement(RolePolicy.AuditLog)));
                config.AddPolicy( RolePolicy.RegisterUser.ToString(), 
                    policy => policy.Requirements.Add(new PageAuthorizedRequirement(RolePolicy.RegisterUser)));
                config.AddPolicy( RolePolicy.RegisterRole.ToString(), 
                    policy => policy.Requirements.Add(new PageAuthorizedRequirement(RolePolicy.RegisterRole)));
                config.AddPolicy( RolePolicy.Version.ToString(), 
                    policy => policy.Requirements.Add(new PageAuthorizedRequirement(RolePolicy.Version)));
                config.AddPolicy( RolePolicy.SystemLog.ToString(), 
                    policy => policy.Requirements.Add(new PageAuthorizedRequirement(RolePolicy.SystemLog)));
            });
            
            services.AddRazorPages();
            services.AddServerSideBlazor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsEnvironment("local") || env.IsEnvironment("dev"))
            {
                app.UseDeveloperExceptionPage();
            }
            
            if(!env.IsEnvironment("local"))
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                
                app.Use((context, next) =>
                {
                    context.Request.Scheme = "https";
                    return next();
                });
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();
            
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}