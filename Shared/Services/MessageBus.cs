using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using CTO.Price.Shared.Extensions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CTO.Price.Shared.Services
{
    [ExcludeFromCodeCoverage]
    public sealed class KafkaBusOption
    {
        public string GroupId { get; set; } = string.Empty;
        public string Bootstrap { get; set; } = string.Empty;
        public string SslCaLocation { get; set; } = string.Empty;
        public string SslCertificateLocation { get; set; } = string.Empty;
        public string SslKeyLocation { get; set; } = string.Empty;
        public bool SslAuthen => !(string.IsNullOrEmpty(SslCaLocation) || string.IsNullOrEmpty(SslCertificateLocation) || string.IsNullOrEmpty(SslKeyLocation));
        public int MaxPollIntervalMs { get; set; } = 600000;
        public int FetchMaxBytes  { get; set; } = 1048576;
        public int MaxPartitionFetchBytes  { get; set; } = 1048576;
    }

    [ExcludeFromCodeCoverage]
    public sealed class MessageBusOption
    {
        public string PriceImport { get; set; } = string.Empty;
        public string PriceAnnouncement { get; set; } = string.Empty;
        public string WarmUpTopic { get; set; } = string.Empty;
    }

    public interface IMessageBus
    {
        ITopicChannel ListenTopic(string topic);
        ITopicPublisher CreatePublisher();
    }
    
    [ExcludeFromCodeCoverage]
    public sealed class MessageBus : IMessageBus
    {
        readonly IOptionsMonitor<KafkaBusOption> busOption;
        readonly ILogger<MessageBus> logger;
        public MessageBus(IOptionsMonitor<KafkaBusOption> busOption, ILogger<MessageBus> logger) {
            this.busOption = busOption;
            this.logger = logger;
        }

        public ITopicChannel ListenTopic(string topic) {
            var opt = busOption.CurrentValue;
            var config = new ConsumerConfig
            {
                GroupId = opt.GroupId,
                BootstrapServers = opt.Bootstrap,
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false
            };
            if (opt.SslAuthen) {
                config.SecurityProtocol = SecurityProtocol.Ssl;
                config.SslCaLocation = opt.SslCaLocation;
                config.SslCertificateLocation = opt.SslCertificateLocation;
                config.SslKeyLocation = opt.SslKeyLocation;
                config.MaxPollIntervalMs = opt.MaxPollIntervalMs;
                config.MessageMaxBytes = opt.FetchMaxBytes;
                config.MaxPartitionFetchBytes = opt.MaxPartitionFetchBytes;
            }
            var consumer = new ConsumerBuilder<string, string>(config)
                .SetLogHandler((_, message) => logger.Log((LogLevel) message.LevelAs(LogLevelType.MicrosoftExtensionsLogging), message.Message))
                .SetErrorHandler((_, error) => logger.LogError(new KafkaException(error), error.Reason))
                .Build();
            consumer.Subscribe(topic);
            logger.LogInformation(config.ToJsonString());
            return new TopicChannel(consumer);
        }

        public ITopicPublisher CreatePublisher() {
            var opt = busOption.CurrentValue;
            var config = new ProducerConfig
            {
                BootstrapServers = opt.Bootstrap,
                Acks = Acks.Leader,
                LingerMs = 50
            };
            if (opt.SslAuthen) {
                config.SecurityProtocol = SecurityProtocol.Ssl;
                config.SslCaLocation = opt.SslCaLocation;
                config.SslCertificateLocation = opt.SslCertificateLocation;
                config.SslKeyLocation = opt.SslKeyLocation;
                config.EnableSslCertificateVerification = false;
            }
            return new TopicPublisher(new ProducerBuilder<string,string>(config)
                .SetLogHandler((_, message) => logger.Log((LogLevel) message.LevelAs(LogLevelType.MicrosoftExtensionsLogging), message.Message))
                .SetErrorHandler((_, error) => logger.LogError(new KafkaException(error), error.Reason))
                .Build());
        }
    }

}