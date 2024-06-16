using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CTO.Price.Shared.Extensions
{
    public static class IEnumerableExtension
    {
        public static async Task ForEach<T>(this IEnumerable<T> seq, Func<T, Task> handler)
        {
            await Task.WhenAll(seq.Select(handler));
        }

        public static long CountMatchData<T>(this IEnumerable<T> lookUp, IEnumerable<T> data)
        {
            var lookUpArray = lookUp as T[] ?? lookUp.ToArray();
            var dataArray = data as T[] ?? data.ToArray();
            var matchFirstPositions = lookUpArray.Select((l, i) => Equals(l, dataArray[0]) ? i : -1)
                .Where(i => i != -1).ToArray();
            
            var matchCount = default(long);
            foreach (var position in matchFirstPositions)
            {
                if (position + dataArray.Length > lookUpArray.Length) continue;
                
                var i = 0;
                var isMatch = true;
                while (isMatch && i < dataArray.Length)
                {
                    if (Equals(lookUpArray[position + i], dataArray[i])) {
                        i++;
                    }
                    else {
                        isMatch = false;
                    }
                }

                if (isMatch) matchCount++;
            }

            return matchCount;
        }
    }
}