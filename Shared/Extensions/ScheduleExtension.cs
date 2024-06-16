using System;
using System.Collections.Generic;
using System.Linq;

using CTO.Price.Shared.Domain;
using RZ.Foundation;
using RZ.Foundation.Extensions;

namespace CTO.Price.Shared.Extensions
{
    public static class ScheduleExtension
    {
        public static SchedulePriceUpdate CombineSchedule(this SchedulePriceUpdate current, SchedulePriceUpdate incoming) => new SchedulePriceUpdate()
        {
            OriginalPrice = current.OriginalPrice.CombineScheduledPriceDescription(incoming.OriginalPrice),
            SalePrice = current.SalePrice.CombineScheduledPriceDescription(incoming.SalePrice),
            PromotionPrice = current.PromotionPrice.CombineScheduledPriceDescription(incoming.PromotionPrice),
            AdditionalData = current.AdditionalData?.SideEffect(curAdditionalData => incoming.AdditionalData?.ForEach(pair => curAdditionalData[pair.Key] = pair.Value )) ?? incoming.AdditionalData
        };

        private static PriceDescription? CombineScheduledPriceDescription(this PriceDescription? current, PriceDescription? incoming) {
            
            if (current == null && incoming == null)
                return null;

            PriceDescription mostRecent;
            if (current != null && incoming != null)
                mostRecent = current.UpdateTime > incoming.UpdateTime ? current : incoming;
            else
                mostRecent = (current ?? incoming)!;

            return mostRecent;
        }

        public static PriceModel ToPriceModel(this Schedule schedule) => new PriceModel()
        {
            Key = new PriceModelKey(schedule.Key.Channel, schedule.Key.Store, schedule.Key.Sku),
            OriginalPrice = schedule.PriceUpdate.OriginalPrice.Try(op => op.SideEffect(p =>
            {
                p.Start = schedule.Key.StartDate;
                p.End = schedule.Key.EndDate;
                p.UpdateTime = op.UpdateTime;
            })),
            SalePrice = schedule.PriceUpdate.SalePrice.Try(sp => sp.SideEffect(p =>
            {
                p.Start = schedule.Key.StartDate;
                p.End = schedule.Key.EndDate;
                p.UpdateTime = sp.UpdateTime;
            })),
            PromotionPrice = schedule.PriceUpdate.PromotionPrice.Try(pp => pp.SideEffect(p =>
            {
                p.Start = schedule.Key.StartDate;
                p.End = schedule.Key.EndDate;
                p.UpdateTime = pp.UpdateTime;
            })),
            AdditionalData = schedule.PriceUpdate.AdditionalData,
            PriceTime = new []{schedule.PriceUpdate.OriginalPrice?.UpdateTime, schedule.PriceUpdate.SalePrice?.UpdateTime, schedule.PriceUpdate.PromotionPrice?.UpdateTime}.Select(dt => dt ?? DateTime.MinValue).Max()
        };

        public static Schedule CopyFrom(this Schedule schedule, Schedule source)
        {
            return new Schedule
            {
                Key = new ScheduleKey(source.Key.StartDate, source.Key.EndDate, source.Key.Channel, source.Key.Store, source.Key.Sku),
                PriceUpdate = new SchedulePriceUpdate
                {
                    OriginalPrice = source.PriceUpdate.OriginalPrice.Try(p =>
                        new PriceDescription
                        {
                            Vat = p.Vat,
                            NonVat = p.NonVat,
                            Start = p.Start,
                            End = p.End,
                            UpdateTime = p.UpdateTime
                        }),
                    SalePrice = source.PriceUpdate.SalePrice.Try(p =>
                        new PriceDescription
                        {
                            Vat = p.Vat,
                            NonVat = p.NonVat,
                            Start = p.Start,
                            End = p.End,
                            UpdateTime = p.UpdateTime
                        }),
                    PromotionPrice = source.PriceUpdate.PromotionPrice.Try(p =>
                        new PriceDescription
                        {
                            Vat = p.Vat,
                            NonVat = p.NonVat,
                            Start = p.Start,
                            End = p.End,
                            UpdateTime = p.UpdateTime
                        }),
                },
                Status = source.Status,
                LastUpdate = source.LastUpdate
            };
        }
    }
}