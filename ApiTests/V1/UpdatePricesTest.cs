using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Akka.TestKit.Xunit2;
using CTO.Price.Api.Services;
using CTO.Price.Proto.V1;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Extensions;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Moq;
using TestUtility;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ApiTests.V1
{
    public class UpdatePricesTest : TestKit
    {
        readonly TestBed<PriceApiServiceV1> testBed;

        public UpdatePricesTest(ITestOutputHelper output)
        {
            testBed = new TestBed<PriceApiServiceV1>(output);
            testBed.RegisterSingleton<IPerformanceCounter>(new PerformanceCounter());
        }

        [Fact]
        public async Task UpdatePrices_FileUploadFailure_ThrowInvalidArgument()
        {
            //Arrange
            var chunk = new Chunk
            {
                Content = ByteString.CopyFrom("filename\n This is the sentence for mock chunk", Encoding.Unicode)
            };
            var chunks = new List<Chunk>() {chunk};
            var requestStream = new ServerStreamMock<Chunk>(chunks);
            
            testBed.Fake<IFileStorageService>()
                .Setup(s => s.Upload(It.IsAny<string>(), It.IsAny<Stream>()))
                .Throws(new InvalidOperationException("Couldn't upload file"));
            var priceApiServiceV1 = testBed.CreateSubject();
            
            const string expectExpDetail = "Status(StatusCode=Unavailable, Detail=\"Couldn't upload file\")";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV1.UpdatePrices(requestStream, It.IsAny<ServerCallContext>());
            };
            
            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.Unavailable
                                                                       && e.Message == expectExpDetail);
        }

        [Fact]
        public async Task UpdatePrices_FileUploadDataInvalid_ThrowInvalidArgument()
        {
            //Arrange
            var content = "filename\n";
            content += "bu,channel,store,sku,online_price,online_from_date,online_price_enabled,online_to_date,jda_discount_code" + Environment.NewLine;
            content += ",,,,,,,,";
            var chunk = new Chunk
            {
                Content = ByteString.CopyFrom(content, Encoding.Default)
            };
            var chunks = new List<Chunk>() {chunk};
            var requestStream = new ServerStreamMock<Chunk>(chunks);

            var key = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc).ToIsoFormat();

            testBed.Fake<IFileStorageService>()
                .Setup(s => s.Upload(It.IsAny<string>(), It.IsAny<Stream>()))
                .ReturnsAsync(key);
            var priceApiServiceV1 = testBed.CreateSubject();
            
            const string expectExpDetail = "Status(StatusCode=InvalidArgument, Detail=\"upload file contain empty record\")";

            //Act
            Func<Task> act = async () =>
            {
                await priceApiServiceV1.UpdatePrices(requestStream, It.IsAny<ServerCallContext>());
            };

            //Assert
            (await act.Should().ThrowAsync<RpcException>()).Where(e => e.Status.StatusCode == StatusCode.InvalidArgument
                                                                       && e.Message == expectExpDetail);
        }

        [Fact]
        public async Task UpdatePrices_FileUploadDataValid_UploadSuccess()
        {
            //Arrange
            var content = "filename\n";
            content += "bu,channel,store,sku,online_price,online_from_date,online_price_enabled,online_to_date,jda_discount_code" + Environment.NewLine;
            content += "CDS,CDS-Website,10138,CDS-0001,200,10-01-2020,yes,10-31-2020,jda_disc";
            var chunk = new Chunk
            {
                Content = ByteString.CopyFrom(content, Encoding.Default)
            };
            var chunks = new List<Chunk>() {chunk};
            var requestStream = new ServerStreamMock<Chunk>(chunks);

            var key = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc).ToIsoFormat();

            testBed.Fake<IFileStorageService>()
                .Setup(s => s.Upload(It.IsAny<string>(), It.IsAny<Stream>()))
                .ReturnsAsync(key);
            testBed.Fake<ITopicPublisher>()
                .Setup(s => s.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<TimeSpan>()));
            testBed.Fake<IOptions<MessageBusOption>>()
                .Setup(s => s.Value).Returns(new MessageBusOption
                {
                    WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
                });
            testBed.Fake<IOptions<PriceDefaults>>()
                .Setup(s => s.Value).Returns(new PriceDefaults{ VatRate = 7});

            var priceApiServiceV1 = testBed.CreateSubject();
            
            //Act
            var result = await priceApiServiceV1.UpdatePrices(requestStream,
                It.IsAny<ServerCallContext>());

            //Assert
            result.GetType().Should().Be(typeof(Empty));
        }

        [Fact]
        public async Task UpdatePrices_FileUploadDataValid_CallFileStorageService()
        {
            //Arrange
            var content = "filename\n";
            content += "bu,channel,store,sku,online_price,online_from_date,online_price_enabled,online_to_date,jda_discount_code" + Environment.NewLine;
            content += "CDS,CDS-Website,10138,CDS-0001,200,10-01-2020,yes,10-31-2020,jda_disc";
            var chunk = new Chunk
            {
                Content = ByteString.CopyFrom(content, Encoding.Default)
            };
            var chunks = new List<Chunk>() {chunk};
            var requestStream = new ServerStreamMock<Chunk>(chunks);

            var key = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc).ToIsoFormat();
            
            var fileStorageService = new Mock<IFileStorageService>();
            fileStorageService.Setup(s => s.Upload(It.IsAny<string>(), It.IsAny<Stream>()))
                .ReturnsAsync(key);
            testBed.RegisterSingleton(fileStorageService.Object);
            
            testBed.Fake<ITopicPublisher>()
                .Setup(s => s.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<TimeSpan>()));
            testBed.Fake<IOptions<MessageBusOption>>()
                .Setup(s => s.Value).Returns(new MessageBusOption
                {
                    WarmUpTopic = "WARMUP-TOPIC", PriceImport = "IMPORT-TOPIC", PriceAnnouncement = "PUBLISH-TOPIC"
                });
            testBed.Fake<IOptions<PriceDefaults>>()
                .Setup(s => s.Value).Returns(new PriceDefaults{ VatRate = 7});
            
            var priceApiServiceV1 = testBed.CreateSubject();
            
            //Act
            await priceApiServiceV1.UpdatePrices(requestStream, It.IsAny<ServerCallContext>());
            
            //Assert
            fileStorageService.Verify(s => s.Upload(It.IsAny<string>(), It.IsAny<Stream>()), Times.Once);
        }
    }
}