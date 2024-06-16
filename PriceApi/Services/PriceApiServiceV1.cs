using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CTO.Price.Proto.V1;
using CTO.Price.Shared.Actor;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Extensions;
using CTO.Price.Shared.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RZ.Foundation;
using RZ.Foundation.Extensions;
using static RZ.Foundation.Prelude;

using DomainPriceDescription = CTO.Price.Shared.Domain.PriceDescription;

using ProtoPriceDateRange = CTO.Price.Proto.V1.GetPricesReply.Types.PriceInfo.Types.PriceDetailInfo.Types.PriceDateRange;
using ProtoPriceValue = CTO.Price.Proto.V1.PriceValue;
using ProtoPriceDescription = CTO.Price.Proto.V1.PriceDescription;
using ProtoPriceReplyInfo = CTO.Price.Proto.V1.GetPricesReply.Types.PriceInfo.Types.PriceDetailInfo;

namespace CTO.Price.Api.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class PriceApiServiceV1 : PriceApi.PriceApiBase
    {
        static readonly TimeSpan PublishTimeout = 30.Seconds();
        readonly IPriceService priceService;
        readonly IScheduleService scheduleService;
        readonly ITopicPublisher publisher;
        readonly MessageBusOption busOption;
        readonly PriceDefaults priceDefaults;
        readonly IPerformanceCounter perfCounter;
        readonly IEventLogService eventLogger;
        readonly IFileStorageService fileStorageService;

        public PriceApiServiceV1(IScheduleService scheduleService, IPriceService priceService,
            ITopicPublisher topicPublisher,
            IOptions<MessageBusOption> busOption, IOptions<PriceDefaults> priceDefaults,
            IPerformanceCounter perfCounter, IEventLogService eventLogger, IFileStorageService fileStorageService)
        {
            this.scheduleService = scheduleService;
            this.priceService = priceService;
            this.publisher = topicPublisher;
            this.busOption = busOption.Value;
            this.priceDefaults = priceDefaults.Value;
            this.perfCounter = perfCounter;
            this.eventLogger = eventLogger;
            this.fileStorageService = fileStorageService;
        }

        #region Public methods

        public override async Task<GetPricesReply> GetPrices(GetPricesParam request, ServerCallContext context)
        {
            var keys = request.Skus.Select(sku => new PriceModelKey(request.Channel, request.Store, sku));
            var (prices, notInCache) = await perfCounter.CollectPerformance(CodeBlock.GetPrices,
                async () => await priceService.GetBaseAndChannelPrices(keys));

            var priceReply = new GetPricesReply
            {
                Version = "1",
                UnknownSkus = {notInCache},
                Details =
                {
                    from price in prices
                    let details = PriceUpdatedEvent.FromModel(price.BasePrice, price.ChannelPrice).Details
                    let primaryPrice = price.ChannelPrice.GetOrElse(price.BasePrice.Get)
                    select new GetPricesReply.Types.PriceInfo
                    {
                        Bu = request.Bu,
                        Channel = request.Channel,
                        Store = request.Store,
                        Sku = primaryPrice.Key.Sku,
                        Details = new ProtoPriceReplyInfo
                        {
                            VatRate = primaryPrice.VatRate.ToIcString(),
                            Price = new ProtoPriceValue()
                            {
                                Vat = details.Price.Vat.ToIcString(),
                                NonVat = details.Price.NonVat.ToIcString()
                            },
                            SpecialPrice = Optional(details.SpecialPrice).Map
                            (
                                sp => new ProtoPriceValue
                                {
                                    Vat = sp.Vat.ToIcString(),
                                    NonVat = sp.NonVat.ToIcString()
                                }
                            ).GetOrDefault(),
                            SpecialPeriod = Optional(details.SpecialPeriod).Map
                            (
                                sp => new ProtoPriceDateRange
                                {
                                    Start = sp.Start?.ToTimestamp(),
                                    End = sp.End?.ToTimestamp()
                                }
                            ).GetOrDefault()
                        },
                        AdditionalData = primaryPrice.AdditionalData.Try(JsonConvert.SerializeObject) ?? string.Empty
                    }
                }
            };

            return priceReply;
        }

        public override async Task<Empty> UpdatePrice(PriceUpdateParam request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Channel))
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    "Invalid payload detected, Missing Channel parameter."));
            if (string.IsNullOrWhiteSpace(request.Store))
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    "Invalid payload detected, Missing Store parameter."));
            if (string.IsNullOrWhiteSpace(request.Sku))
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    "Invalid payload detected, Missing SKU parameter."));
            if (request.PromotionPrice == null && request.SalePrice == null && request.OriginalPrice == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    $"Invalid payload detected, {new PriceModelKey(request.Channel, request.Store, request.Sku)} has no prices."));

            request.GetType().GetProperties().Where(p => p.PropertyType == typeof(ProtoPriceDescription)).ForEach(p =>
            {
                Optional((ProtoPriceDescription?) p.GetValue(request)).Then(pd =>
                {
                    if (string.IsNullOrEmpty(pd.PriceVat) && string.IsNullOrEmpty(pd.PriceNonVat))
                        throw new RpcException(new Status(StatusCode.InvalidArgument,
                            $"Invalid payload detected, {p.Name} field contains no prices."));
                });
            });

            var mapped = ToRawPriceUpdate(request);
            var vatRate = mapped.VatRate ?? priceDefaults.VatRate;
            var model = mapped.ToPriceModel(vatRate, DateTime.UtcNow);

            await publisher.PublishAsync(busOption.PriceImport, model.Key.ToString(),
                JsonConvert.SerializeObject(mapped), PublishTimeout);

            await eventLogger.Information(request.Channel, request.Store, request.Sku,
                EventEnum.UpdatePriceApi, model);

            return await Task.FromResult(new Empty());
        }

        public override async Task GetSchedules(GetSchedulesParam request,
            IServerStreamWriter<GetSchedulesReply> responseStream, ServerCallContext context)
        {
            var schedulerEnumerator = scheduleService.GetSchedules(request.Channel, request.Store, request.Sku).GetAsyncEnumerator();
            var tasks = new List<Task>();
            while (await schedulerEnumerator.MoveNextAsync())
            {
                var schedule = schedulerEnumerator.Current;
                tasks.Add(
                    responseStream.WriteAsync(new GetSchedulesReply
                    {
                        Start = Optional(schedule.Key.StartDate).Map(d => d.ToTimestamp()).ToNullable(),
                        End = Optional(schedule.Key.EndDate).Map(d => d.ToTimestamp()).ToNullable(),
                        OriginalPriceSchedule = Optional(schedule.PriceUpdate.OriginalPrice).Map(ToPriceValue).ToNullable(),
                        SalePriceSchedule = Optional(schedule.PriceUpdate.SalePrice).Map(ToPriceValue).ToNullable(),
                        PromotionPriceSchedule = Optional(schedule.PriceUpdate.PromotionPrice).Map(ToPriceValue).ToNullable()
                    })
                );
            }

            await Task.WhenAll(tasks);

            static ProtoPriceValue ToPriceValue(DomainPriceDescription priceDescription) => new ProtoPriceValue
            {
                Vat = priceDescription.Vat.ToIcString(),
                NonVat = priceDescription.NonVat.ToIcString()
            };
        }

        public override async Task<Empty> DeleteSchedule(DeleteScheduleParam request, ServerCallContext context)
        {
            var key = new ScheduleKey(request.Start?.ToDateTime(), request.End?.ToDateTime(), request.Channel,
                request.Store, request.Sku);

            await (await TryAsync(() => scheduleService.DeleteSchedule(key)).Try()).Get(
                async r =>
                {
                    await eventLogger.Information(request.Channel, request.Store,
                        request.Sku, EventEnum.DeleteSchedule, key);
                },
                async e =>
                {
                    await eventLogger.Error(request.Channel, request.Store,
                        request.Sku, EventEnum.DeleteSchedule, key);
                    throw new RpcException(new Status(StatusCode.FailedPrecondition, e.Message));
                });

            return new Empty();
        }

        public override async Task<Empty> UpdatePrices(IAsyncStreamReader<Chunk> requestStream,
            ServerCallContext context)
        {
            try
            {
                var chunks = requestStream.ReadAllAsync();
                var stream = new MemoryStream();

                byte[] fileContent = { };
                fileContent = await chunks.AggregateAsync(fileContent, (current, chunk) => current.Concat(chunk.Content.ToByteArray()).ToArray());

                var (headerContent, dataContent) = SeparateContent(fileContent, Encoding.UTF8.GetBytes("\n"));
                var fileName = Encoding.UTF8.GetString(headerContent);
                
                stream.Write(dataContent, 0, dataContent.Length);

                (await TryAsync(() => fileStorageService.Upload(fileName, stream)).Try())
                    .GetOrElse(exception =>
                        throw new RpcException(new Status(StatusCode.Unavailable, exception.Message)));

                var records = CsvProvider.GetPrices(stream.ToArray()).ToList();

                var duplicatePriceKey = records.Select(r => new PriceModelKey(r.Channel, r.Store, r.Sku))
                    .GroupBy(p => p, (key, keys) => new {Key = key, Result = keys})
                    .Where(g => g.Result.ToArray().Length > 1)
                    .Select(g => g.Key.ToString()).Take(10).ToList();
                if (duplicatePriceKey.Count > 0)
                    throw new RpcException(new Status(StatusCode.InvalidArgument,
                        $"Invalid payload detected, Duplicate channel,store,sku|{string.Join("|", duplicatePriceKey)}"));

                var invalids = new List<string>();
                var index = 0;
                var keys = records.Select(r => new PriceModelKey(r.Channel, r.Store, r.Sku));
                var (prices, notInCache) = await perfCounter.CollectPerformance(CodeBlock.GetPrices,
                async () => await priceService.GetBaseAndChannelPrices(keys));

                var pricesIndex = 0;
                while (pricesIndex < prices.Length)
                {
                    var price = prices[pricesIndex];

                    if (price.BasePrice.IsSome)
                    {
                        var onlinePrices = records.Where(record => record.Sku == price.BasePrice.Get().Key.Sku)
                                                  .Select(record => record.OnlinePrice)
                                                  .ToList();

                        foreach (var onlinePrice in onlinePrices)
                        {
                            if (price.BasePrice.Get().OriginalPrice != null && onlinePrice.HasValue && onlinePrice.Value > price.BasePrice.Get().OriginalPrice.Vat)
                            {
                                invalids.Add($"sku:{price.BasePrice.Get().Key.Sku} OnlinePrice:{onlinePrice.Value} is greater than OriginalPrice:{price.BasePrice.Get().OriginalPrice.Vat}");
                            }
                            if (price.BasePrice.Get().SalePrice != null && onlinePrice.HasValue && onlinePrice.Value > price.BasePrice.Get().SalePrice.Vat)
                            {
                                invalids.Add($"sku:{price.BasePrice.Get().Key.Sku} OnlinePrice:{onlinePrice.Value} is greater than SalePrice:{price.BasePrice.Get().SalePrice.Vat}");
                            }
                            if(invalids.Count > 100) {
                                break;
                            }
                        }
                    }

                    pricesIndex++;
                }
                if (invalids.Any())
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid payload detected: {string.Join(", ", invalids)}"));
                }

                while (index < records.Count && invalids.Count < 10)
                {
                    var record = records[index];

                    invalids.AddRange(from propertyInfo in record.GetType().GetProperties()
                        where propertyInfo.PropertyType == typeof(string)
                        let value = (string) propertyInfo.GetValue(record)!
                        where string.IsNullOrEmpty(value)
                        select $"row {index + 1}: field {propertyInfo.Name} can not be empty");

                    if (record.OnlineFromDate >= record.OnlineToDate)
                        invalids.Add($"row {index + 1}: online_from_date date MUST be before online_to_date date");

                    if (!record.OnlinePriceEnabled.IsIn(new[] {"yes", "no"}))
                        invalids.Add($"row {index + 1}: online_price_enabled({record.OnlinePriceEnabled}) must be yes or no");

                    if (record.OnlinePrice <= 0 || record.OnlinePrice is null)
                        invalids.Add($"row {index + 1}: online_price({record.OnlinePrice}) can not be empty, negative or zero.");

                    if (!record.ValidateOnlinePriceDigits(2))
                        invalids.Add($"row {index + 1}: online_price({record.OnlinePrice}) can have only 2 digits");

                    index++;
                }

                if (invalids.Any())
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument,
                        $"Invalid payload detected at row(s)|{string.Join("|", invalids)}" +
                        (invalids.Count == 10 ? "|and may have more..." : string.Empty)));
                }

                records.ForEach(PublishPrices);

                return new Empty();
            }
            catch (Exception exception)
            {
                var detailedErrorMessage = $"{exception.Message}\nStack Trace: {exception.StackTrace}";

                throw exception switch
                {
                    RpcException rpcException => rpcException,
                    _ => new RpcException(new Status(StatusCode.FailedPrecondition, detailedErrorMessage))
                };
            }

            void PublishPrices(UploadedPrice uploadedPrice)
            {
                var vatRate = priceDefaults.VatRate;
                var mapped = uploadedPrice.ToRawPrice(vatRate, UpdateVersion, UpdateEvent, DateTime.UtcNow);
                var model = mapped.ToPriceModel(vatRate, DateTime.Now);
                publisher.Publish(busOption.PriceImport, model.Key.ToString(),
                    JsonConvert.SerializeObject(mapped), _ => { });
            }

            static (byte[], byte[]) SeparateContent(byte[] content, byte[] mark)
            {
                var header = new List<byte>();
                var data = new List<byte>();

                var i = 0;
                var isEndOfHeader = false;
                while (!isEndOfHeader)
                {
                    var chkContent = new byte[mark.Length];
                    for (int j = 0; j < mark.Length; j++) {
                        chkContent[j] = content[i + j];
                    }

                    if (chkContent.SequenceEqual(mark)) {
                        isEndOfHeader = true;
                        i += mark.Length;
                    }
                    else {
                        header.Add(content[i]);
                        i++;
                    }
                }

                for (int j = i; j < content.Length; j++) {
                    data.Add(content[j]);
                }
                return (header.ToArray(), data.ToArray());
            }
        }

        public override async Task<PriceMetrics> GetPriceMetrics(Empty empty, ServerCallContext context)
        {
            return new PriceMetrics()
            {
                TotalPrices = await priceService.TotalPriceCount(),
                TotalSchedules = await scheduleService.TotalScheduleCount(),
                TotalPendingStartSchedules = await scheduleService.TotalPendingStartSchedulesCount(),
                TotalPendingEndSchedules = await scheduleService.TotalPendingEndSchedulesCount()
            };
        }

        #endregion

        #region Private methods

        private const string UpdateVersion = "price.v1";
        private const string UpdateEvent = "price.raw";

        private RawPrice ToRawPriceUpdate(PriceUpdateParam priceUpdateParam)
        {
            var vatRate = string.IsNullOrEmpty(priceUpdateParam.VatRate)
                ? priceDefaults.VatRate
                : decimal.Parse(priceUpdateParam.VatRate);
            return new RawPrice
            {
                Version = UpdateVersion,
                Channel = priceUpdateParam.Channel,
                Store = priceUpdateParam.Store,
                Sku = priceUpdateParam.Sku,
                Event = UpdateEvent,
                Timestamp = priceUpdateParam.Timestamp?.ToDateTime() ?? DateTime.UtcNow,
                VatRate = vatRate,
                OriginalPrice = Optional(priceUpdateParam.OriginalPrice).Map(p => ToRawPriceDescription(p, vatRate))
                    .GetOrDefault(),
                SalePrice = Optional(priceUpdateParam.SalePrice).Map(p => ToRawPriceDescription(p, vatRate))
                    .GetOrDefault(),
                PromotionPrice = Optional(priceUpdateParam.PromotionPrice).Map(p => ToRawPriceDescription(p, vatRate))
                    .GetOrDefault()
            };

            static RawPriceDescription
                ToRawPriceDescription(ProtoPriceDescription priceDescription, decimal vatRate) =>
                new RawPriceDescription
                {
                    PriceVat = string.IsNullOrEmpty(priceDescription.PriceVat)
                        ? decimal.Parse(priceDescription.PriceNonVat).ToVatPrice(vatRate)
                        : decimal.Parse(priceDescription.PriceVat),
                    PriceNonVat = string.IsNullOrEmpty(priceDescription.PriceNonVat)
                        ? decimal.Parse(priceDescription.PriceVat).ToNonVatPrice(vatRate)
                        : decimal.Parse(priceDescription.PriceNonVat),
                    Start = Optional(priceDescription.Start).Map(d => d.ToDateTime()).ToNullable(),
                    End = Optional(priceDescription.End).Map(d => d.ToDateTime()).ToNullable(),
                };
        }

        #endregion
    }
}