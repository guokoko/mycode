using System.Collections.Generic;
using System.Linq;

namespace CTO.Price.Shared.Extensions
{
    public static class StringExtension
    {
        public static bool IsIn(this string str, IEnumerable<string> list) => list.Contains(str);
    }
}