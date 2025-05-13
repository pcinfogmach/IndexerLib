using IndexerLib.IndexManger;
using IndexerLib.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using IndexerLib.Tokens;

namespace IndexerLib.IndexManager
{
    public static class Search
    {
        public static List<SearchResult> UnorderedProximitySearch(string query, int proximity = 3)
        {
            var fileGroups = new Dictionary<string, Dictionary<string, Postings[]>>();
            var terms = query.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            var tokens = new Dictionary<string, List<Token>>();
            using (var indexReader = new IndexReader())
            {
                foreach (var term in terms)
                {
                    if (!tokens.ContainsKey(term))
                        tokens[term] = new List<Token>();

                    var data = indexReader.Get(term);
                    if (data != null)
                    {
                        var deserialized = TokenSerializer.Deserialize(data).ToList();
                        foreach (var entry in deserialized)
                            tokens[term].Add(entry);
                    }
                }
            }

            foreach (var term in terms)
            {
                foreach (var token in tokens[term])
                {
                    if (!fileGroups.ContainsKey(token.ID))
                        fileGroups[token.ID] = new Dictionary<string, Postings[]>();

                    fileGroups[token.ID][term] = token.Postings.OrderBy(p => p.Position).ToArray(); // Ensure sorted
                }
            }

            var results = new List<SearchResult>();

            foreach (var fileEntry in fileGroups)
            {
                var filePath = fileEntry.Key;
                var termPostings = fileEntry.Value;

                if (termPostings.Count < terms.Length)
                    continue;

                var iterators = terms
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

                        // Advance all queues (can tune this)
                        for (int i = 0; i < iterators.Count; i++)
                        {
                            iterators[i].Dequeue();
                        }
                    }
                    else
                    {
                        // Advance the queue with the smallest position
                        int minIndex = activeWindow
                            .Select((p, idx) => (p, idx))
                            .OrderBy(t => t.p.Position)
                            .First().idx;

                        iterators[minIndex].Dequeue();
                    }
                }
            }

            return results;
        }
    }
}
