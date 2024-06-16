using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CTO.Price.Api
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static string Version =>
            Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion;

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    // {
                        var bindingPort = int.Parse(Environment.GetEnvironmentVariable("BindingPort") ??
                                                    throw new Exception("Please define BindingPort EV"));
                        webBuilder.ConfigureKestrel(opt =>
                        {
                            // for gRPC, Mac only support HTTP2
                            opt.ListenAnyIP(bindingPort, o => o.Protocols = HttpProtocols.Http2);
                            // for Web API
                            opt.ListenAnyIP(bindingPort + 1, o => o.Protocols = HttpProtocols.Http1AndHttp2);
                        });
                    // }

                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog();
    }
}