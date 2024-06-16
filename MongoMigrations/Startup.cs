using CTO.Price.Shared.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mongo.Migration.Documents;
using Mongo.Migration.Startup;
using Mongo.Migration.Startup.DotNetCore;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using IApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;

namespace WebApplication
{
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
            BsonClassMap.RegisterClassMap<PriceModel>(cm => {
                cm.AutoMap();
                cm.MapIdMember(pm => pm.Key);
            });
            services.AddSingleton<IMongoClient>(new MongoClient());
            services.AddMigration(new MongoMigrationSettings
            {
                ConnectionString = Configuration.GetSection("Storage:ConnectionString").Value,
                Database = Configuration.GetSection("Storage:Database").Value,
                DatabaseMigrationVersion = new DocumentVersion(1,0,2)
            });
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IHostApplicationLifetime lifetime)
        {
            lifetime.StopApplication();
        }
    }
}