using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace SharedTests
{
    public class FileStorageTest
    {
        readonly TestBed<FileStorageService> testBed;

        public FileStorageTest(ITestOutputHelper output)
        {
            testBed = new TestBed<FileStorageService>(output);
        }

        [Fact]
        public async Task FileStorageService_GetHttpStatusCodeOk_ReturnKey()
        {
            //Arrange
            const string key = "key-test";
            
            var stream = new MemoryStream();
            var response = new PutObjectResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            };

            testBed.Fake<IAmazonS3>()
                .Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            testBed.Fake<IOptions<AwsSettings>>()
                .Setup(s => s.Value).Returns(new AwsSettings
                {
                    Region = "ap-southeast-1",
                    S3 = new AwsSettings.S3Settings {Bucket = "cg-common-price-service"}
                });
            var fileStorageService = testBed.CreateSubject();

            //Act
            var result = await fileStorageService.Upload(key, stream);

            //Arrange
            result.Should().Be(key);
        }

        [Fact]
        public async Task FileStorageService_GetHttpStatusCodeLocked_ThrowInvalidOperationException()
        {
            //Arrange
            const string key = "key-test";
            
            var stream = new MemoryStream();
            var response = new PutObjectResponse
            {
                HttpStatusCode = HttpStatusCode.Locked
            };

            testBed.Fake<IAmazonS3>()
                .Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            testBed.Fake<IOptions<AwsSettings>>()
                .Setup(s => s.Value).Returns(new AwsSettings
                {
                    Region = "ap-southeast-1",
                    S3 = new AwsSettings.S3Settings {Bucket = "cg-common-price-service"}
                });
            var fileStorageService = testBed.CreateSubject();
            
            //Act
            Func<Task> act = async () =>
            {
                await fileStorageService.Upload(key, stream);
            };

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Couldn't upload file");
        }

        [Fact]
        public void FileStorageService_GeneratePreSignedUrl_ShouldCallGetPreSignedURLAndReturnDownloadLink()
        {
            // Arrange
            const string key = "key-test";
            const string link = "www.s3keydownloadlink.com";
            
            var amazonClient = new Mock<IAmazonS3>();
            amazonClient
                .Setup(s => s.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
                .Returns(link);
            testBed.RegisterSingleton(amazonClient.Object);

            testBed.Fake<IOptions<AwsSettings>>()
                .Setup(s => s.Value).Returns(new AwsSettings
                {
                    Region = "ap-southeast-1",
                    S3 = new AwsSettings.S3Settings {Bucket = "cg-common-price-service"}
                });
            var fileStorageService = testBed.CreateSubject();
            
            // Act
            var result = fileStorageService.GeneratePreSignedUrl(key);
            
            // Assert
            result.Should().Be(link);
            amazonClient.Verify(v => v.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()), Times.Once);
        }
    }
}