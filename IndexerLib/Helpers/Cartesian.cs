using System.Collections.Generic;
using System.Linq;

namespace IndexerLib.Helpers
{
    public static class Cartesian
    {
        public static IEnumerable<List<string>> Product(List<string[]> sequences)
        {
            IEnumerable<List<string>> result = new[] { new List<string>() };

            foreach (var sequence in sequences)
            {
                result = from acc in result
                         from item in sequence
                         select new List<string>(acc) { item };
            }

            return result;
        }
    }
}
