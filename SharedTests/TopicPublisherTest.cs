using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Moq;
using RZ.Foundation.Extensions;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SharedTests
{
    public class TopicPublisherTest
    {
        private readonly TestBed<TopicPublisher> testBed;

        public TopicPublisherTest(ITestOutputHelper testOutputHelper)
        {
            testBed = new TestBed<TopicPublisher>(testOutputHelper);
        }

        [Fact]
        public async Task TopicPublisherIsWorking_CallPublishAsync_CallProduceAsyncAndReturnResult()
        {
            // Arrange
            var timeout = 5.Seconds();
            const string topic = "TestTopic";
            const string key = "TestKey";
            const string value = "TestValue";
            const long offSetValue = 5;
            var message = new Message<string, string>()
            {
                Key = key,
                Value = value
            };
            var consumer = new Mock<IProducer<string, string>>();
            consumer.Setup(s => s.ProduceAsync(topic,
                    It.Is<Message<string, string>>(m => m.Key == message.Key && m.Value == message.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeliveryResult<string, string>()
                {
                    Offset = new Offset(offSetValue)
                });
            testBed.RegisterSingleton(consumer.Object);

            // Act
            var topicChannel = testBed.CreateSubject();
            var result = await topicChannel.PublishAsync(topic, key, value, timeout);

            // Assert
            consumer.Verify(
                s => s.ProduceAsync(topic,
                    It.Is<Message<string, string>>(m => m.Key == message.Key && m.Value == message.Value),
                    It.IsAny<CancellationToken>()), Times.Once);
            result.Should().Be(offSetValue);
        }

        [Fact]
        public void TopicPublisherIsWorking_CallPublish_CallProduce()
        {
            // Arrange
            var timeout = 5.Seconds();
            const string topic = "TestTopic";
            const string key = "TestKey";
            const string value = "TestValue";
            var message = new Message<string, string>()
            {
                Key = key,
                Value = value
            };

            var handler = new Mock<Action<DeliveryReport<string, string>>>();

            var consumer = new Mock<IProducer<string, string>>();
            consumer.Setup(s => s.Produce(topic,
                    It.Is<Message<string, string>>(m => m.Key == message.Key && m.Value == message.Value),
                    handler.Object));
            
            testBed.RegisterSingleton(consumer.Object);

            // Act
            var topicChannel = testBed.CreateSubject();
            topicChannel.Publish(topic, key, value, handler.Object);

            // Assert
            consumer.Verify(
                s => s.Produce(topic,
                    It.Is<Message<string, string>>(m => m.Key == message.Key && m.Value == message.Value),
                    handler.Object), Times.Once);
        }

        [Fact]
        public void TopicPublisherIsWorking_Disposed_DisposeProducer()
        {
            var consumer = new Mock<IProducer<string, string>>();
            testBed.RegisterSingleton(consumer.Object);

            // Act
            var topicChannel = testBed.CreateSubject();
            topicChannel.Dispose();

            // Assert
            consumer.Verify(s => s.Dispose(), Times.Once);
        }
    }
}