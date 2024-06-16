using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTO.Price.Proto.V2;
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
using Chunk = CTO.Price.Proto.V2.Chunk;
using DeleteScheduleParam = CTO.Price.Proto.V2.DeleteScheduleParam;
using DomainPriceDescription = CTO.Price.Shared.Domain.PriceDescription;
using GetPricesReply = CTO.Price.Proto.V2.GetPricesReply;
using GetSchedulesParam = CTO.Price.Proto.V2.GetSchedulesParam;
using GetSchedulesReply = CTO.Price.Proto.V2.GetSchedulesReply;
using PriceApi = CTO.Price.Proto.V2.PriceApi;
using PriceMetrics = CTO.Price.Proto.V2.PriceMetrics;
using PriceUpdateParam = CTO.Price.Proto.V2.PriceUpdateParam;
using ProtoPriceDateRange = CTO.Price.Proto.V2.GetPricesReply.Types.PriceInfo.Types.PriceDetailInfo.Types.PriceDateRange;
using ProtoPriceValue = CTO.Price.Proto.V2.PriceValue;
using ProtoPriceDescription = CTO.Price.Proto.V2.PriceDescription;
using ProtoPriceReplyInfo = CTO.Price.Proto.V2.GetPricesReply.Types.PriceInfo.Types.PriceDetailInfo;

namespace CTO.Price.Api.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class PriceApiServiceV2 : PriceApi.PriceApiBase
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

        public PriceApiServiceV2(IScheduleService scheduleService, IPriceService priceService,
            ITopicPublisher topicPublisher,
            IOptions<MessageBusOption> busOption, IOptions<PriceDefaults> priceDefaults,
            IPerformanceCounter perfCounter, IEventLogService eventLogger, IFileStorageService fileStorageService)
        {
            this.scheduleService = scheduleService;
            this.priceService = priceService;
            publisher = topicPublisher;
            this.busOption = busOption.Value;
            this.priceDefaults = priceDefaults.Value;
            this.perfCounter = perfCounter;
            this.eventLogger = eventLogger;
            this.fileStorageService = fileStorageService;
        }

        #region Public methods

        public override async Task<GetPricesReply> GetPricesBySku(GetPricesBySkuParam request, ServerCallContext context)
        {
            var systemKeys = await priceService.GetPriceModelKeys(request.Sku);

            var (prices, _) = await perfCounter.CollectPerformance(CodeBlock.GetPrices, async () => await priceService.GetBaseAndChannelPrices(systemKeys));

            var priceReply = BindingPricesReplyData(null, prices, null, null);

            return priceReply;
        }
            
        public override async Task<GetPricesReply> GetPricesByStore(GetPricesByStoreParam request, ServerCallContext context)
        {
            var keys = request.Skus.Select(sku => new PriceModelKey(null, request.Store, sku));
            var (prices, notInCache) = await perfCounter.CollectPerformance(CodeBlock.GetPrices,
                async () => await priceService.GetBaseAndChannelPrices(keys));

            var priceReply = BindingPricesReplyData(notInCache, prices, "", request.Store);

            return priceReply;
        }

        public override async Task<GetPricesReply> GetPricesByChannel(GetPricesByChannelParam request, ServerCallContext context)
        {
            var keys = request.Skus.Select(sku => new PriceModelKey(request.Channel, request.Store, sku));
            var (prices, notInCache) = await perfCounter.CollectPerformance(CodeBlock.GetPrices,
                async () => await priceService.GetBaseAndChannelPrices(keys));

            var priceReply = BindingPricesReplyData(notInCache, prices, request.Channel, request.Store);

            return priceReply;
        }

        public override async Task<Empty> UpdatePrice(PriceUpdateParam request, ServerCallContext context)
        {
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
            var schedulerEnumerator = scheduleService.GetSchedules(request.Channel, request.Store, request.Sku)
                .GetAsyncEnumerator();
            var tasks = new List<Task>();
            while (await schedulerEnumerator.MoveNextAsync())
            {
                var schedule = schedulerEnumerator.Current;
                tasks.Add(
                    responseStream.WriteAsync(new GetSchedulesReply
                    {
                        Start = Optional(schedule.Key.StartDate).Map(d => d.ToTimestamp()).ToNullable(),
                        End = Optional(schedule.Key.EndDate).Map(d => d.ToTimestamp()).ToNullable(),
                        OriginalPriceSchedule =
                            Optional(schedule.PriceUpdate.OriginalPrice).Map(ToPriceValue).ToNullable(),
                        SalePriceSchedule = Optional(schedule.PriceUpdate.SalePrice).Map(ToPriceValue).ToNullable(),
                        PromotionPriceSchedule =
                            Optional(schedule.PriceUpdate.PromotionPrice).Map(ToPriceValue).ToNullable()
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
            var chunks = requestStream.ReadAllAsync();
            var stream = new MemoryStream();

            await foreach (var chunk in chunks)
            {
                var contentBytes = chunk.Content.ToByteArray();
                stream.Write(contentBytes, 0, contentBytes.Length);
            }

            (await TryAsync(() => fileStorageService.Upload(DateTime.UtcNow.ToIsoFormat(), stream)).Try())
                .GetOrElse(exception =>
                    throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message)));

            var records = CsvProvider.GetPrices(stream.ToArray()).ToList();
            var invalids = new List<int>();

            foreach (var (record, index) in records.Select((item, index) => (item, index)))
            {
                if (string.IsNullOrWhiteSpace(record.Channel) ||
                    string.IsNullOrWhiteSpace(record.Store) ||
                    string.IsNullOrWhiteSpace(record.Sku) ||
                    record.OnlinePrice == null)
                {
                    invalids.Add(index + 1);
                }
            }

            if (invalids.Any())
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    $"Invalid payload detected at row(s) {string.Join(", ", invalids)}"));
            }

            var uploadTasks = records.Select(UpdatePrices);

            await Task.WhenAll(uploadTasks);
            return new Empty();

            async Task UpdatePrices(UploadedPrice uploadedPrice)
            {
                var vatRate = priceDefaults.VatRate;
                var mapped = uploadedPrice.ToRawPrice(vatRate, UpdateVersion, UpdateEvent, DateTime.UtcNow);
                var model = mapped.ToPriceModel(vatRate, DateTime.Now);
                await publisher.PublishAsync(busOption.PriceImport, model.Key.ToString(),
                    JsonConvert.SerializeObject(mapped), PublishTimeout);
                await eventLogger.Information(uploadedPrice.Channel,
                    uploadedPrice.Store, uploadedPrice.Sku, EventEnum.UpdatePrice, model);
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

        private const string UpdateVersion = "price.v2";
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

        private GetPricesReply BindingPricesReplyData(string[]? notInCache, BaseAndChannelPrice[] prices, string? channel, string? store) => new GetPricesReply
            {
                Version = "1",
                UnknownSkus = {notInCache ?? new string[0]},
                Details =
                {
                    from price in prices
                    let details = PriceUpdatedEvent.FromModel(price.BasePrice, price.ChannelPrice).Details
                    let primaryPrice = price.ChannelPrice.GetOrElse(price.BasePrice.Get)
                    select new GetPricesReply.Types.PriceInfo
                    {
                        Channel = channel ?? primaryPrice.Key.Channel,
                        Store = store ?? primaryPrice.Key.Store,
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
        
        #endregion
    }
}