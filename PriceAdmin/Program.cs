using System;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace CTO.Price.Admin
{
    public class Program
    {
        public static string Version => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        // MacOS lacks cipher suite needed for dotnet 3 to properly use HTTP/2, so this check is needed.
        static bool IsRunningOnMac => bool.Parse(Environment.GetEnvironmentVariable("IsMacOS") ?? "false");

        public static void Main(string[] args)
        {
            if (IsRunningOnMac)
                // configure HTTP2 connection to exclude TLS so .NET gRPC works on Mac
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStaticWebAssets();
                    webBuilder.UseStartup<Startup>();
                });
    }
}
