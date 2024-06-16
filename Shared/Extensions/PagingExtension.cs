using System;
using System.Collections.Generic;
using System.Linq;

namespace CTO.Price.Shared.Extensions
{
    public static class PagingExtension
    {
        public static List<string> GetPaginationSettingBySemiColon(string setting) => setting.Split(';').ToList();
    }
}