using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using CTO.Price.Shared.Domain;
using Newtonsoft.Json;
using RZ.Foundation.Extensions;

namespace Producer
{
    class Program
    {
        const string CtoKafka = "kafka-bootstrap.k8s-stg2.datalake.central.tech:443";
        // const string TargetTopic = "push.products.price.updated-test-local";
        static readonly ImmutableDictionary<string, string> TargetTopic = ImmutableDictionary<string, string>.Empty.AddRange(new[]
        {
            new KeyValuePair<string, string>("DEV", "push.products.price.updated-dev"),
            new KeyValuePair<string, string>("SIT", "push.products.price.updated-sit"),
            new KeyValuePair<string, string>("UAT", "push.products.price.updated-uat"),
            new KeyValuePair<string, string>("LTE", "push.products.price.updated-lte")
        });

        static async Task Main() {
            var config = new ProducerConfig
            {
                BootstrapServers = CtoKafka,
                SecurityProtocol = SecurityProtocol.Ssl,
                SslCaLocation = "../../../../../../DevKeys/truststore.cer.pem",
                SslCertificateLocation = "../../../../../../DevKeys/keystore.crt",
                SslKeyLocation = "../../../../../../DevKeys/keystore.key"
            };
            
            SetTargetTopic:
            Console.WriteLine("Set target to");
            TargetTopic.ForEach(t => Console.WriteLine($"{t}"));
            var topicInput = (Console.ReadLine() ?? string.Empty).ToUpper();
            var topicSelect = TargetTopic.ContainsKey(topicInput) ? TargetTopic[topicInput] : null;
            if (string.IsNullOrEmpty(topicSelect))
                goto SetTargetTopic;
            Console.WriteLine($"Choose {topicInput} Set targetTopic to {topicSelect}");

            using var p = new ProducerBuilder<string, string>(config).Build();
            await p.ProduceAsync("ignore.me", new Message<string, string>());

            while (await Run(p, Menu(), topicSelect)) { /* continue */ }

            Console.WriteLine("Done.");
        }

        static char Menu() {
            Console.WriteLine("1. Send one raw price.");
            Console.WriteLine("2. Manual submit raw price.");
            Console.WriteLine("3. Raw price load test.");
            Console.Write("Select (Q to quit): ");
            return Console.ReadKey(intercept: true).KeyChar;
        }

        static async Task<bool> Run(IProducer<string, string> p, char selected, string targetTopic) {
            switch (selected) {
                case '1':
                    await SendOne(p, targetTopic);
                    return true;
                case '2':
                    await LoopManualSend(p, targetTopic);
                    return true;
                case '3':
                    await LoadTest(p, targetTopic);
                    return true;
                case 'Q':
                case 'q':
                    return false;
            }

            return true;
        }

        static async Task LoopManualSend(IProducer<string,string> p, string targetTopic) {
            Console.WriteLine("Enter text to send... Blank to quit");
            string input;
            while (!string.IsNullOrEmpty(input = Prompt()))
                await p.ProduceAsync(targetTopic, new Message<string, string> {Value = input});
        }

        static string Prompt() {
            Console.Write("Input: ");
            return Console.ReadLine();
        }

        static int _sendCount;
        static readonly object LockProcess = new object();
        
#pragma warning disable 1998
        static async Task LoadTest(IProducer<string,string> p, string targetTopic) {
            Console.WriteLine("Start generating load, any key to stop...");
            
            const int maximum = 100000;
            const int maxRound = 30;
            const int storeLength = 25;
            const int timeInSleep = 10000;
            var store = "10100";

            using var stopToken = new CancellationTokenSource();
            // var sendTasks = Enumerable.Range(1, maximum).Select(r => SendOneWithSku(p, targetTopic, "10138", r)).ToArray();
            // await Task.WhenAll(sendTasks);
            for (var round = 1; round <= storeLength; round++)
            {
                _sendCount = 0;
                store = (Convert.ToInt32(store) + 1).ToString();
                SendSkuInOneStore(store, maximum, maxRound);
                Thread.Sleep(timeInSleep);
            }

            void SendSkuInOneStore(string storeP, int maximumP, int maxRoundP)
            {
                var watch = Stopwatch.StartNew();
                for (var round = 1; round <= maxRoundP; round++)
                {
                    lock (LockProcess)
                    {
                        var sendTasks = Enumerable.Range(1, maximumP)
                            .Select(r => SendOneWithSku(p, targetTopic, storeP, CalcMark(round, maximumP, r))).ToArray();
                        Task.WhenAll(sendTasks);
                        Thread.Sleep(timeInSleep);
                        Console.WriteLine($"Done send data round {round}/{maxRoundP} with store {storeP}");
                    }
                }
                var time = watch.Elapsed.TotalSeconds;
                Console.WriteLine("Produced {0} in {1} secs, tps = {2}", _sendCount, time, _sendCount / time);
            }

            int CalcMark(int round, int max, int rt) => ((round - 1) * max) + rt;
        }
#pragma warning restore 1998


        static async Task LoopSend(IProducer<string, string> p, string targetTopic, CancellationToken cancelToken) {
            while (!cancelToken.IsCancellationRequested) {
                var payload = JsonConvert.SerializeObject(Payload(DateTime.UtcNow));
                await NotifyPrice(p, targetTopic, payload);
            }
        }
        
        static async Task SendOne(IProducer<string, string> p, string targetTopic) {
            var payload = JsonConvert.SerializeObject(Payload(DateTime.UtcNow));
            Console.WriteLine($"send : {payload}");
            await NotifyPrice(p, targetTopic, payload);
        }

        static async Task SendOneWithSku(IProducer<string, string> p, string targetTopic, string store, int skuMark) {
            var payload = JsonConvert.SerializeObject(Payload(DateTime.UtcNow, store, skuMark));
            // Console.WriteLine($"send : {payload}");
            await NotifyPrice(p, targetTopic, payload);
        }
        
        static async Task NotifyPrice(IProducer<string,string> p, string targetTopic, string payload) {
            await p.ProduceAsync(targetTopic, new Message<string, string> {Value = payload});
            Interlocked.Increment(ref _sendCount);
        }

        static readonly Random MyRandom = new Random();
        static RawPrice Payload(DateTime dateTime, string store, int skuMark)
        {
            const string skuTemp = "SKU00000000";
            var skuTempLength = skuTemp.Length;
            var skuMarkLength = skuMark.Length();
            var skuGenerate = skuTemp.Left(skuTempLength - skuMarkLength) + skuMark;
            
            var originalPrice = MyRandom.Next(100000);
            var specialPrice = Convert.ToInt32(originalPrice * 0.8);
            
            return new RawPrice
            {
                Version = "price.v2",
                Event = "price.raw",
                // Channel = "CDS-Website",
                Store = store,
                Sku = skuGenerate,
                VatRate = 7,
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = originalPrice,
                },
                SalePrice = new RawPriceDescription
                {
                    PriceVat = specialPrice,
                },
                Timestamp = dateTime
            };
        }
        
        static RawPrice Payload(DateTime dateTime)
        {
            return new RawPrice
            {
                Version = "price.v1",
                Event = "price.raw",
                // Channel = "CDS-Website",
                Store = "10138",
                Sku = $"SKU{MyRandom.Next(10000000)}",
                OriginalPrice = new RawPriceDescription
                {
                    PriceVat = MyRandom.Next(100000),
                },
                Timestamp = dateTime
            };
        }
    }

    public static class Extension
    {
        public static string Right(this string input, int index) => input.Substring(input.Length - index, index);
        public static string Left(this string input, int index) => input.Substring(0, index);

        public static int Length(this int input)
        {
            var moder = 1;
            var result = 1;
            while (input / moder > 1)
            {
                moder *= 10;
                result++;
            }

            return result;
        }
    }
}