using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using CTO.Price.Proto.V1;
using CTO.Price.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using PriceApiClientV1 = CTO.Price.Proto.V1.PriceApi.PriceApiClient;

namespace GrpcClient
{
    class Program
    {
        const string endpointLocal = "http://localhost:4772";
        const string endpointDev = "https://priceservice-dev.central.tech:443";
        
        public static async Task Main(string[] args)
        {
            try
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                
                var httpClientHandler = new HttpClientHandler();
                var cert = new X509Certificate2("../../../../../DevKeys/Thawte_RSA_CA_2018.crt");
                httpClientHandler.ClientCertificates.Add(cert);
                var httpClient = new HttpClient(httpClientHandler);

                using var channel = GrpcChannel.ForAddress(endpointDev, new GrpcChannelOptions()
                {
                    HttpClient = httpClient
                });
                
                
                var serviceInfoClient = new ServiceInfo.ServiceInfoClient(channel);
                var version = serviceInfoClient.Version(new Empty());
                Console.WriteLine("Service Version: " + version.Version);

                while (await Run(channel, Menu())) { }
                
                Console.WriteLine("Done.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static char Menu() {
            Console.WriteLine("Select method.");
            Console.WriteLine("1. Standard api test.");
            Console.WriteLine("2. Load test api");
            Console.WriteLine("3. Check Performance");
            Console.WriteLine("4. Reset Performance");
            Console.Write("Select (Q to quit): ");
            return Console.ReadKey(intercept: true).KeyChar;
        }

        static async Task<bool> Run(GrpcChannel channel, char selected) {
            Console.WriteLine(selected);
            switch (selected) {
                case '1' :
                    await CallStandardApi(channel);
                    return true;
                case '2' :
                    await CallLoadTestApi(channel);
                    return true;
                case '3' :
                    await CallGetStatPerformance(channel);
                    return true;
                case '4' :
                    await CallResetStatPerformance(channel);
                    return true;
                case 'Q' :
                    return false;
            }
            return true;
        }

        static async Task CallGetStatPerformance(GrpcChannel channel) {
            var performanceInfoClient = new PerformanceInfo.PerformanceInfoClient(channel);

            var result = await performanceInfoClient.GetCounterAsync(new Empty());
            WritePerformanceResult(result);
        }

        static async Task CallResetStatPerformance(GrpcChannel channel) {
            var performanceInfoClient = new PerformanceInfo.PerformanceInfoClient(channel);

            var result = await performanceInfoClient.ResetCounterAsync(new Empty());
            WritePerformanceResult(result);
        }

        static void WritePerformanceResult(PerformanceReply result) {
            Console.WriteLine($"inbound = {result.Inbound}");
            Console.WriteLine($"outbound = {result.Outbound}");
            Console.WriteLine($"getApiPerformance = {string.Join(Environment.NewLine, result.GetApiPerformance)}");
            Console.WriteLine($"ips = {result.Ips}");
            Console.WriteLine($"ops = {result.Ops}");
            Console.WriteLine($"since = {result.Since}");
        }

        #region CallLoadTestApi

        static int sendCount;

        static async Task CallLoadTestApi(GrpcChannel channel) {
            Console.WriteLine("Start generating load, any key to stop...");
            Console.WriteLine($"At:{DateTime.Now.ToString("O", CultureInfo.InvariantCulture)}");
            sendCount = 0;
            
            using var stopToken = new CancellationTokenSource();
            var watch = Stopwatch.StartNew();
            var sendTasks = Enumerable.Range(1, 512).Select(_ => LoopSend(channel, stopToken.Token)).ToArray();
            Console.ReadKey();
            stopToken.Cancel();
            await Task.WhenAll(sendTasks);
            var time = watch.Elapsed.TotalSeconds;
            Console.WriteLine("Produced {0} in {1} secs, tps = {2}", sendCount, time, sendCount / time);
        }

        static async Task LoopSend(GrpcChannel channel, CancellationToken cancelToken) {
            while (!cancelToken.IsCancellationRequested) {
                var apiClient = new PriceApiClientV1(channel);
                var priceParam = new GetPricesParam
                {
                    Bu = "CDS", Channel = "CDS-Website", Store = "10138", Skus = {"CDS11300502", "CDS11470953"}
                };
                await CallPriceApi(apiClient, priceParam);
            }
        }

        static async Task CallPriceApi(PriceApiClientV1 client, GetPricesParam param) {
            try {
                await client.GetPricesAsync(param);
                Interlocked.Increment(ref sendCount);
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion
        
        #region CallStandApi

        static async Task CallStandardApi(GrpcChannel channel) {
            try {
                var apiClient = new PriceApiClientV1(channel);
                
                var getPriceResult = apiClient.GetPrices(
                    new GetPricesParam()
                    {
                        Bu = "CDS", Channel = "CDS-Website", Store = "10138", Skus = {"186599", "497663"}
                    }
                );
                Console.WriteLine($"GetPriceResult: {getPriceResult}");
                
                var updatePriceResult = apiClient.UpdatePrice(new PriceUpdateParam()
                {
                    Bu = "CDS",
                    Channel = "CDS-Website",
                    Store = "10138",
                    Sku = "807878",
                    OriginalPrice = new PriceDescription()
                    {
                        PriceVat = "100"
                    },
                    Timestamp = DateTimeOffset.Now.ToTimestamp(),
                });
                Console.WriteLine($"UpdatePriceResult: {updatePriceResult}");
                
                var getSchedule = apiClient.GetSchedules(new GetSchedulesParam()
                {
                    Bu = "CDS",
                    Channel = "CDS-Website",
                    Store = "10138",
                    Sku = "807878"
                });
                await foreach (var schedule in getSchedule.ResponseStream.ReadAllAsync())
                {
                    Console.WriteLine($"Schedule {schedule}");    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        #endregion
    }
}