using IndexerLib.Index;
using IndexerLib.IndexManager;
using IndexerLib.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexerLib.IndexManger
{
    public static class Search
    {
        public static IEnumerable<SnippetCollection> GetSnippets(string query, short proximity = 3)
        {
            var results = UnorderedProximitySearch(query, proximity)
                .GroupBy(r => r.FileId);

            Console.WriteLine("Generating Snippets..." + DateTime.Now);
            using (var idStore = new IdStore())
                foreach (var result in results)
                    yield return SnippetGenerator.Generate(idStore, result);
        }

        public static List<SearchResult> UnorderedProximitySearch(string query, short proximity = 3)
        {
            Console.WriteLine("Parsing query..." + DateTime.Now);
            var termGroups = QueryParser.Parse(query);

            if (termGroups.Count > 3)
                proximity = (short)(proximity * (termGroups.Count - 2) + termGroups.Count);
            else
                proximity = (short)(proximity + termGroups.Count);

                Console.WriteLine("Querying index..." + DateTime.Now);
            var tokens = GetTokens(termGroups);
            Console.WriteLine("Grouping by doc..." + DateTime.Now);
            var docGroups = GroupByDocId(tokens);
            Console.WriteLine("Generating results..." + DateTime.Now);
            return GetResults(docGroups, proximity);
        }

        //tokens are groups by order of termgroups
        static Dictionary<int, List<Token>> GetTokens(List<List<string>> termGroups)
        {
            var tokenDict = new Dictionary<int, List<Token>>();
            using (var reader = new IndexReader(true))
            {
                for (int i = 0; i < termGroups.Count; i++)
                {
                    tokenDict[i] = new List<Token>();
                    foreach (var term in termGroups[i])
                    {
                        var data = reader.Get(term);
                        if (data != null)
                            tokenDict[i].AddRange(TokenSerializer.Deserialize(data));
                    }
                }
            }
            return tokenDict;
        }

        static Dictionary<int, List<Token>> GroupByDocId(Dictionary<int, List<Token>> tokens)
        {
            var resultGroups = new Dictionary<int, List<Token>>();
            foreach (var tokenGroup in tokens)
            {
                var idGroups = tokenGroup.Value.GroupBy(t => t.ID);
                foreach (var idGroup in idGroups)
                {
                    var postings = idGroup.SelectMany(t => t.Postings).OrderBy(p => p.Position);

                    if (!resultGroups.ContainsKey(idGroup.Key))
                        resultGroups[idGroup.Key] = new List<Token>();

                    resultGroups[idGroup.Key].Add(new Token
                    {
                        ID = idGroup.Key,
                        Postings = postings.ToList()
                    });

                }
            }
            return resultGroups;
        }

        static List<SearchResult> GetResults(Dictionary<int, List<Token>> docGroups, short proximity)
        {
            var results = new List<SearchResult>();

            foreach (var docEntry in docGroups)
            {
                var postingsLists = docEntry.Value
                    .Select(token => token.Postings)
                    .Where(p => p.Count > 0)
                    .ToList();

                // Skip documents that don't have all term groups
                if (postingsLists.Count != docEntry.Value.Count)
                    continue;

                var indices = new int[postingsLists.Count];
                var matches = new List<Postings[]>();

                while (true)
                {
                    var currentPositions = new List<(int position, int listIndex)>();

                    for (int i = 0; i < postingsLists.Count; i++)
                    {
                        if (indices[i] >= postingsLists[i].Count)
                            goto End; // One list exhausted, done with this document

                        currentPositions.Add((postingsLists[i][indices[i]].Position, i));
                    }

                    var min = currentPositions.Min(x => x.position);
                    var max = currentPositions.Max(x => x.position);

                    if (max - min <= proximity)
                    {
                        // Collect current combination
                        matches.Add(indices.Select((idx, i) => postingsLists[i][idx]).ToArray());

                        // Advance all pointers (to find more matches)
                        for (int i = 0; i < indices.Length; i++)
                            indices[i]++;
                    }
                    else
                    {
                        // Advance the pointer for the list with the smallest position
                        int minIndex = currentPositions.First(x => x.position == min).listIndex;
                        indices[minIndex]++;
                    }
                }

            End:
                if (matches.Count > 0)
                {
                    results.Add(new SearchResult
                    {
                        FileId = docEntry.Key,
                        MatchedPostings = matches
                    });
                }
            }

            return results;
        }

    }
}
