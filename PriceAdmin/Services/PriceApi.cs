using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorInputFile;
using CTO.Price.Proto.V1;
using CTO.Price.Protos;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;

namespace CTO.Price.Admin.Services
{
    public interface IPriceApi : IDisposable
    {
        Task<string> GetVersion();
        Task<string> UpdatePrices(string fileName, IFileListEntry file);
        Task<PriceMetrics> GetPriceMetrics();
    }

    [ExcludeFromCodeCoverageAttribute]
    public sealed class PriceApi : IPriceApi
    {
        readonly GrpcChannel channel;

        public PriceApi(IOptionsMonitor<ServiceUris> serviceUris)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress(serviceUris.CurrentValue.Api);
        }

        public void Dispose()
        {
            channel.Dispose();
        }

        public async Task<string> GetVersion()
        {
            var client = new ServiceInfo.ServiceInfoClient(channel);
            var response = await client.VersionAsync(new Empty());
            return response.Version;
        }

        private const int ChunkSize = 2048;

        public async Task<string> UpdatePrices(string fileName, IFileListEntry file)
        {
            // validate the records in the csv file before sending to PriceApi
            //send records to PriceApi with stream
            var client = new Proto.V1.PriceApi.PriceApiClient(channel);
            using var call = client.UpdatePrices();
            
            await using (var stream = new MemoryStream())
            {
                var header = Encoding.UTF8.GetBytes($"{fileName}\n");
                var buffer = new byte[ChunkSize]; // read in chunks of 2KB
                int bytesRead;
                while ((bytesRead = await file.Data.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                }

                var result = header.Concat(stream.ToArray()).ToArray(); 
                await call.RequestStream.WriteAsync(new Chunk()
                {
                    Content = ByteString.CopyFrom(result)
                });
            }

            await call.RequestStream.CompleteAsync();
            await call.ResponseAsync;
            return "Uploaded successfully!";
        }

        public async Task<PriceMetrics> GetPriceMetrics()
        {
            var client = new Proto.V1.PriceApi.PriceApiClient(channel);
            var response = await client.GetPriceMetricsAsync(new Empty());
            return response;
        }
    }
}