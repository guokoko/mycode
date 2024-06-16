using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CTO.Price.Proto.V1;
using CTO.Price.Protos;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IntegrationTests
{
    public class UpdatePriceTest
    {
        const string SettingsFileName = "appsettings.json";
        readonly HttpClient httpClient;
        readonly IConfiguration configuration;

        public UpdatePriceTest()
        {
            configuration = InitConfiguration();

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var cert = new X509Certificate2(configuration["CertificatePath"]);
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ClientCertificates.Add(cert);
            httpClient = new HttpClient(httpClientHandler);

            static IConfiguration InitConfiguration() => new ConfigurationBuilder().AddJsonFile(SettingsFileName).Build();
        }

        private GrpcChannel CreateChannel() => GrpcChannel.ForAddress(configuration["ApiEndpoint"], new GrpcChannelOptions() {HttpClient = httpClient});
        
        [Fact]
        public async Task UpdatePrices_PriceUpdated()
        {
            //Arrange
            const string bu = "CDS";
            const string channel = "CDS-Website";
            const string store = "10138";
            const string sku = "CDS0001";
            const string priceVat = "107";
            
            using var grpcChannel = CreateChannel();
            var priceApiClient = new PriceApi.PriceApiClient(grpcChannel);
            
            //Act
            var initialPriceQueryResult = priceApiClient.GetPrices(new GetPricesParam() {Bu = bu, Channel = channel, Store = store, Skus = {sku}});
            
            //Assert
            initialPriceQueryResult.Details.Should().BeEmpty();
            
            //Act
            priceApiClient.UpdatePrice(new PriceUpdateParam()
            {
                Bu = bu,
                Channel = channel,
                Store = store,
                Sku = sku,
                OriginalPrice = new PriceDescription()
                {
                    PriceVat = priceVat,
                    End = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(30))
                }
            });
            
            await Task.Delay(10000);
            var afterPriceUpdateQueryResult = priceApiClient.GetPrices(new GetPricesParam() {Bu = bu, Channel = channel, Store = store, Skus = {sku}});
            
            //Assert
            afterPriceUpdateQueryResult.Details.Should().HaveCount(1);
        }
    }
}