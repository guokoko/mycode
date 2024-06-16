using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CTO.Price.Proto.V2;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;

namespace IntegrationTests.Helpers
{
    public class PriceApiHelper
    {
        const string SettingsFileName = "appsettings.json";
        readonly HttpClient httpClient;
        readonly IConfiguration configuration;

        public PriceApiHelper()
        {
            configuration = InitConfiguration();

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var cert = new X509Certificate2(configuration["CertificatePath"]);
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ClientCertificates.Add(cert);
            httpClient = new HttpClient(httpClientHandler);

            static IConfiguration InitConfiguration() => new ConfigurationBuilder().AddJsonFile(SettingsFileName).Build();
        }

        const int TimespanAfterUpdatePrice = 10000;

        GrpcChannel CreateChannel() => GrpcChannel.ForAddress(configuration["ApiEndpoint"], new GrpcChannelOptions() {HttpClient = httpClient});

        public async Task UpdatePrice(string? channel, string store, string sku, string? originalPrice,
            string? salePrice, string? promotionPrice, DateTime priceEnd) 
            => await UpdatePrice(channel, store, sku, originalPrice, salePrice, promotionPrice, priceEnd, TimespanAfterUpdatePrice);

        public async Task UpdatePrice(string? channel, string store, string sku, string? originalPrice,
            string? salePrice, string? promotionPrice, DateTime priceEnd, int delay)
        {
            using var grpcChannel = CreateChannel();
            var priceApiClient = new PriceApi.PriceApiClient(grpcChannel);
            
            priceApiClient.UpdatePrice(new PriceUpdateParam
            {
                Channel = channel ?? string.Empty,
                Store = store,
                Sku = sku,
                OriginalPrice = SetPriceDescription(originalPrice),
                SalePrice = SetPriceDescription(salePrice),
                PromotionPrice = SetPriceDescription(promotionPrice)
            });

            if (delay > 0)
                await Task.Delay(delay);

            PriceDescription? SetPriceDescription(string? price) => string.IsNullOrEmpty(price)
                ? null
                : new PriceDescription
                {
                    PriceVat = price,
                    End = Timestamp.FromDateTime(priceEnd)
                };
        }

        public GetPricesReply GetPrice(string? channel, string store, string sku)
        {
            using var grpcChannel = CreateChannel();
            var priceApiClient = new PriceApi.PriceApiClient(grpcChannel);

            return string.IsNullOrEmpty(channel)
                ? priceApiClient.GetPricesByStore(new GetPricesByStoreParam {Store = store, Skus = {sku}})
                : priceApiClient.GetPricesByChannel(new GetPricesByChannelParam
                    {Channel = channel, Store = store, Skus = {sku}});
        }
    }
}