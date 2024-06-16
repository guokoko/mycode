using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace CTO.Price.Shared.Services
{
    [ExcludeFromCodeCoverageAttribute]
    public class AwsSettings
    {
        public string Region { get; set; } = null!;
        public S3Settings S3 { get; set; } = null!;
        public EsSettings Es { get; set; } = null!;

        public class EsSettings
        {
            public string Uri { get; set; } = null!;
        }

        public class S3Settings
        {
            public string ServiceUrl { get; set; } = null!;
            public string Bucket { get; set; } = null!;
            public int UrlExpired { get; set; } = 10;
        }
    }

    public interface IFileStorageService
    {
        public Task<string> Upload(string key, Stream content);
        public Task<string> Get(string objectKey);
        public string GeneratePreSignedUrl(string objectKey);
    }
    
    public class FileStorageService : IFileStorageService
    {
        readonly IAmazonS3 s3Client;
        private readonly AwsSettings.S3Settings settings;

        public FileStorageService(IAmazonS3 s3Client, IOptions<AwsSettings> settings)
        {
            this.s3Client = s3Client;
            this.settings = settings.Value.S3;
        }

        public async Task<string> Upload(string key, Stream content)
        {
            var request = new PutObjectRequest {
                BucketName = settings.Bucket,
                Key = key,
                InputStream = content
            };
            var response = await s3Client.PutObjectAsync(request);
            if (response.HttpStatusCode != HttpStatusCode.OK) {
                throw new InvalidOperationException("Couldn't upload file");
            }
            return key;
        }

        public async Task<string> Get(string objectKey)
        {
            var response = await s3Client.GetObjectAsync(settings.Bucket, objectKey);
            if (response.HttpStatusCode != HttpStatusCode.OK) {
                throw new InvalidOperationException("Couldn't get file");
            }
            await using Stream responseStream = response.ResponseStream;
            using StreamReader reader = new StreamReader(responseStream);
            return await reader.ReadToEndAsync();
        }

        public string GeneratePreSignedUrl(string objectKey)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = settings.Bucket,
                Key = objectKey,
                Expires = DateTime.UtcNow.AddMinutes(settings.UrlExpired)
            };
            return s3Client.GetPreSignedURL(request);
        }
    }
}