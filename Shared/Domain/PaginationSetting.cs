using System;

namespace CTO.Price.Shared.Domain
{
    public sealed class PaginationSetting
    {
        public string PageSize { get; set; } = string.Empty;
        public int LimitPaginationStep { get; set; } = 0;
        public int MinRequisiteToLimitPagination { get; set; } = 0;
    }
}