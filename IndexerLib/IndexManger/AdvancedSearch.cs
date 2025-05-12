using IndexerLib.Index;
using IndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IndexerLib.IndexManger
{
    public static class AdvancedSearch
    {
        public static List<SearchResult> UnorderedProximitySearch(string query, int proximity = 3)
        {
            query = Regex.Replace(query, @"\s*\|\s*", "|");

            var termGroups = query
                .Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Split('|'))
                .ToList();

            var tokens = new Dictionary<string, List<Token>>();

            using (var indexReader = new IndexReader())
            {
                foreach (var group in termGroups)
                {
                    foreach (var term in group)
                    {
                        if (!tokens.ContainsKey(term))
                            tokens[term] = new List<Token>();

                        var data = indexReader.Get(term);
                        if (data != null)
                        {
                            var deserialized = TokenSerializer.Deserialize(data).ToList();
                            tokens[term].AddRange(deserialized);
                        }
                    }
                }
            }

            var fileGroups = new Dictionary<string, Dictionary<string, Postings[]>>();

            foreach (var group in termGroups)
            {
                foreach (var term in group)
                {
                    foreach (var token in tokens[term])
                    {
                        if (!fileGroups.ContainsKey(token.FilePath))
                            fileGroups[token.FilePath] = new Dictionary<string, Postings[]>();

                        if (!fileGroups[token.FilePath].ContainsKey(term))
                            fileGroups[token.FilePath][term] = token.Postings.OrderBy(p => p.Position).ToArray();
                    }
                }
            }

            var results = new List<SearchResult>();

            foreach (var fileEntry in fileGroups)
            {
                var filePath = fileEntry.Key;
                var termPostings = fileEntry.Value;

                var combinations = CartesianProduct(termGroups);

                foreach (var combo in combinations)
                {
                    if (combo.Any(t => !termPostings.ContainsKey(t)))
                        continue;

                    var iterators = combo
                        .Select(t => termPostings[t])
                        .Select(p => new Queue<Postings>(p))
                        .ToList();

                    var activeWindow = new List<Postings>();

                    while (iterators.All(q => q.Count > 0))
                    {
                        activeWindow.Clear();
                        foreach (var queue in iterators)
                            activeWindow.Add(queue.Peek());

                        int minPos = activeWindow.Min(p => p.Position);
                        int maxPos = activeWindow.Max(p => p.Position);

                        if (maxPos - minPos <= proximity)
                        {
                            var result = results.FirstOrDefault(r => r.FilePath == filePath);
                            if (result == null)
                            {
                                result = new SearchResult { FilePath = filePath };
                                results.Add(result);
                            }

                            result.MatchedPostings.Add(activeWindow.ToArray());

                            for (int i = 0; i < iterators.Count; i++)
                            {
                                iterators[i].Dequeue();
                            }
                        }
                        else
                        {
                            int minIndex = activeWindow
                                .Select((p, idx) => (p, idx))
                                .OrderBy(t => t.p.Position)
                                .First().idx;

                            iterators[minIndex].Dequeue();
                        }
                    }
                }
            }

            return results;
        }

        private static IEnumerable<List<string>> CartesianProduct(List<string[]> sequences)
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
