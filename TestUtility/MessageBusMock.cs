using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using CTO.Price.Shared.Services;
using RZ.Foundation.Extensions;
using static RZ.Foundation.Prelude;

namespace TestUtility
{
    public sealed class MessageBusMock : IMessageBus, ITopicPublisher
    {
        readonly ConcurrentDictionary<string,StringTopicMock> topics = new ConcurrentDictionary<string, StringTopicMock>();

        public void Dispose() {
            // nothing to dispose
        }

        public ITopicChannel ListenTopic(string topic) => GetTopic(topic);

        public ITopicPublisher CreatePublisher() => this;

        public Task<long> PublishAsync(string topic, string key, string value, TimeSpan timeout) =>
            Task.FromResult(GetTopic(topic).Publish(value));

        public void Publish(string topic, string key, string value, Action<DeliveryReport<string, string>> handler)
        {
            Task.FromResult(GetTopic(topic).Publish(value));
        }

        public StringTopicMock GetTopic(string topic) =>
            topics.Get(topic).Get(Identity, () => topics[topic] = new StringTopicMock());
    }
    public sealed class StringTopicMock : ITopicChannel
    {
        readonly ConcurrentQueue<ConsumeResult<string, string>> queue = new ConcurrentQueue<ConsumeResult<string, string>>();

        public int Count => queue.Count;

        public void Dispose() {
            // nothing to dispose
        }

        public ConsumeResult<string, string>? Consume(TimeSpan timeout) =>
            queue.TryDequeue(out var v)
                ? v
                : null;

        public void Commit()
        {
            // commit is handled by kafka
        }

        public long Publish(string value) {
            queue.Enqueue(new ConsumeResult<string, string>()
            {
                Offset = queue.Count,
                Message = new Message<string, string>()
                {
                    Value = value
                }
            });
            return queue.Count;
        }

        public ConsumeResult<string, string>[] RetrieveAll() => IterateQueue().ToArray();

        IEnumerable<ConsumeResult<string, string>> IterateQueue() {
            while (queue.TryDequeue(out var v))
                yield return v;
        }
    }
}