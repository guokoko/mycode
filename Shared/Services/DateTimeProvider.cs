using System;

namespace CTO.Price.Shared.Services
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow();
    }
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow() => DateTime.UtcNow;
    }
}