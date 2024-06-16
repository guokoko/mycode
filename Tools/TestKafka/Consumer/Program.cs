using System;
using System.Diagnostics;
using System.Threading;
using Confluent.Kafka;

namespace Consumer
{
    class Program
    {
        static void Main() {
            var config = new ConsumerConfig
            {
                GroupId = "test-consumer-group1",

                BootstrapServers = "kafka-bootstrap.k8s-stg2.datalake.central.tech:443",
                SecurityProtocol = SecurityProtocol.Ssl,
                SslCaLocation = "../../../../../../DevKeys/truststore.cer.pem",
                SslCertificateLocation = "../../../../../../DevKeys/keystore.crt",
                SslKeyLocation = "../../../../../../DevKeys/keystore.key",
                AutoOffsetReset = AutoOffsetReset.Latest
            };

            const string DefaultSourceTopic = "push.price.service.updated-test";

            Console.WriteLine("Enter source topic (current = {0}):", DefaultSourceTopic);
            var newSource = Console.ReadLine();

            using var c = new ConsumerBuilder<string, string>(config).Build();
            c.Subscribe(string.IsNullOrEmpty(newSource)? DefaultSourceTopic : newSource);

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true;
                cts.Cancel();
            };

            Console.WriteLine("Start reading...");
            try {
                while (true) {
                    var cr = c.Consume(cts.Token);
                    Console.WriteLine("MESSAGE at {1}:\r\n{0}\r\n", cr.Message.Value, cr.TopicPartitionOffset);
                }
            }
            catch (OperationCanceledException) {
                // just quit
                Console.WriteLine("Done.");
            }
            finally {
                c.Close();
            }
        }
    }
}