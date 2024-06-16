using System;
using System.Globalization;

namespace CTO.Price.Shared.Extensions
{
    public static class ToStringExtensions
    {
        public static string ToIcString(this decimal d) => d.ToString(CultureInfo.InvariantCulture);
        public static string ToIsoFormat(this DateTime dateTime) => dateTime.ToString("O", CultureInfo.InvariantCulture);
        public static string ToIsoFormatString(this DateTimeOffset dateTimeOffset) => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);
    }
}