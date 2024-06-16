using System;

namespace CTO.Price.Shared.Domain
{
    public enum PriceErrorCategory
    {
        UnexpectedError,
        DuplicatedCode,
        UpdateFailed,
        DeleteFailed
    }

    public sealed class PriceServiceException : ApplicationException
    {
        public PriceServiceException(PriceErrorCategory category, string message, object? data = null, Exception? cause = null) : base(message, cause) {
            Category = category;
            base.Data["data"] = data;
        }

        public PriceErrorCategory Category { get; }
        public new object? Data => base.Data["data"];
    }
}