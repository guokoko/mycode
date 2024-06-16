using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace CTO.Price.Shared.Services
{
    public interface ITopicPublisher : IDisposable
    {
        /// <summary>
        /// Publish key/value within timeout limit.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timeout"></param>
        /// <returns>Offset number that the value has been queued. (Not sure if it has any use..)</returns>
        Task<long> PublishAsync(string topic, string key, string value, TimeSpan timeout);

        void Publish(string topic, string key, string value, Action<DeliveryReport<string, string>> handler);
    }
    
    public sealed class TopicPublisher : ITopicPublisher
    {
        readonly IProducer<string, string> publisher;
        public TopicPublisher(IProducer<string,string> publisher) {
            this.publisher = publisher;
        }

        public void Dispose() {
            publisher.Dispose();
        }

        public async Task<long> PublishAsync(string topic, string key, string value, TimeSpan timeout)
        {
            using var timeoutToken = new CancellationTokenSource(timeout);
            var result = await publisher.ProduceAsync(topic, new Message<string, string> {Key = key, Value = value}, timeoutToken.Token);
            return result.Offset.Value;
        }
            
        public void Publish(string topic, string key, string value, Action<DeliveryReport<string, string>> handler)
        {
            publisher.Produce(topic, new Message<string, string> {Key = key, Value = value}, handler);
        }
    }
}