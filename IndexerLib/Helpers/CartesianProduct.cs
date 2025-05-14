using System.Collections.Generic;
using System.Linq;

namespace IndexerLib.Helpers
{
    public static class CartesianProduct
    {
        public static IEnumerable<List<T>> Produce<T>(List<List<T>> sequences)
        {
            IEnumerable<List<T>> result = new[] { new List<T>() };

            foreach (var sequence in sequences)
            {
                result = result.SelectMany(
                    acc => sequence,
                    (acc, item) => acc.Concat(new[] { item }).ToList());
            }

            return result;
        }

    }
}
