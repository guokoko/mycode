using System;
using System.Collections.Generic;
using Confluent.Kafka;

namespace CTO.Price.Shared.Services
{
    public interface ITopicChannel : IDisposable
    {
        ConsumeResult<string, string>? Consume(TimeSpan timeout);
        public void Commit();

    }
    
    public sealed class TopicChannel : ITopicChannel
    {
        readonly IConsumer<string, string> consumer;
        public TopicChannel(IConsumer<string,string> consumer) {
            this.consumer = consumer;
        }

        public void Dispose() {
            consumer.Dispose();
        }

        public ConsumeResult<string, string>? Consume(TimeSpan timeout) => consumer.Consume(timeout);
        public void Commit() => consumer.Commit();
        
    }
}