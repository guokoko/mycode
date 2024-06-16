using System.Security.Policy;
using Akka.Routing;
using Confluent.Kafka;

namespace CTO.Price.Shared.Domain
{
    public class KafkaPublishMessage
    {
        public KafkaPublishMessage(PriceModelKey priceKey, string messageKey, string messageContent)
        {
            PriceKey = priceKey;
            MessageKey = messageKey;
            MessageContent = messageContent;
        }

        public string MessageKey { get; set; }
        public string MessageContent { get; set; }
        public PriceModelKey PriceKey { get; set; }
    }
}