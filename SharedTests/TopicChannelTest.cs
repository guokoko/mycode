using Confluent.Kafka;
using CTO.Price.Shared.Services;
using FluentAssertions;
using Moq;
using RZ.Foundation.Extensions;
using TestUtility.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace SharedTests
{
    public class TopicChannelTest
    {
        private TestBed<TopicChannel> testBed;
        public TopicChannelTest(ITestOutputHelper testOutputHelper)
        {
            testBed = new TestBed<TopicChannel>(testOutputHelper);
        }

        [Fact]
        public void TopicChannelIsWorking_CalledConsume_ReturnMessage()
        {
            // Arrange
            var timeout = 5.Seconds();
            var consumer = new Mock<IConsumer<string, string>>();
            var consumeResult = new ConsumeResult<string, string>()
            {
                Message = new Message<string, string>()
            }; 
            consumer.Setup(s => s.Consume(timeout)).Returns(consumeResult);
            testBed.RegisterSingleton(consumer.Object);

            // Act
            var topicChannel = testBed.CreateSubject();
            var result = topicChannel.Consume(timeout);
            
            // Assert
            consumer.Verify(s => s.Consume(timeout), Times.Once);
            result.Should().Be(consumeResult);
        }
        
        [Fact]
        public void TopicChannelIsWorking_Diposed_DisposeConsumer()
        {
            // Arrange
            var consumer = new Mock<IConsumer<string, string>>();
            testBed.RegisterSingleton(consumer.Object);

            // Act
            var topicChannel = testBed.CreateSubject();
            topicChannel.Dispose();
            
            // Assert
            consumer.Verify(s => s.Dispose(), Times.Once);
        }
        
        [Fact]
        public void TopicChannelIsWorking_StoreOffset_UnderlyingStoreOffsetIsCalledOffsetIsStored()
        {
            // Arrange
            var result = new ConsumeResult<string, string>();
            var consumer = new Mock<IConsumer<string, string>>();
            testBed.RegisterSingleton(consumer.Object);

            // Act
            var topicChannel = testBed.CreateSubject();
            topicChannel.Commit();
            
            // Assert
            consumer.Verify(s => s.Commit(), Times.Once);
        }
    }
}