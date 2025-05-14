//using IndexerLib.Helpers;
//using IndexerLib.Index;
//using IndexerLib.IndexManager;
//using IndexerLib.Tokens;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.RegularExpressions;

//namespace IndexerLib.IndexManger
//{
//    public static class Search
//    {
//        public static List<SnippetCollection> GetSnippets(string query, int proximity = 3)
//        {
//            var results = UnorderedProximitySearch(query, proximity)
//                .GroupBy(r => r.FileId);

//            Console.WriteLine("Generating Snippets..." + DateTime.Now);
//            List<SnippetCollection> snippets = new List<SnippetCollection>();
//            using (var idStore = new IdStore())
//                foreach (var result in results)
//                    snippets.Add(SnippetGenerator.Generate(idStore, result));

//            return snippets;
//        }


//        public static List<SearchResult> UnorderedProximitySearch(string query, int proximity = 3)
//        {
//            Console.WriteLine("Parsing query..." + DateTime.Now);

//            var termGroups = QueryParser.Parse(query);
//            proximity = termGroups.Count + proximity;
//            Console.WriteLine("Quereying Index..." + DateTime.Now);
//            var tokens = GetTokens(termGroups);
//            Console.WriteLine("Sorting..." + DateTime.Now);
//            var fileGroups = GetFileGroups(termGroups, tokens);
//            Console.WriteLine("Filtering valid results..." + DateTime.Now);
//            return GetResults(fileGroups, termGroups, proximity);
//        }

//        static List<SearchResult> GetResults(Dictionary<int, Dictionary<string, Postings[]>> fileGroups,
//            List<List<string>> termGroups, int proximity)
//        {
//            var results = new List<SearchResult>();

//            foreach (var fileEntry in fileGroups)
//            {
//                int Id = fileEntry.Key;
//                var termPostings = fileEntry.Value;

//                var combinations = CartesianProduct.Produce(termGroups);

//                foreach (List<string> combo in combinations)
//                {
//                    if (combo.Any(t => !termPostings.ContainsKey(t)))
//                        continue;

//                    var iterators = combo
//                        .Select(t => termPostings[t])
//                        .Select(p => new Queue<Postings>(p))
//                        .ToList();

//                    var activeWindow = new List<Postings>();

//                    while (iterators.All(q => q.Count > 0))
//                    {
//                        activeWindow.Clear();
//                        foreach (var queue in iterators)
//                            activeWindow.Add(queue.Peek());

//                        int minPos = activeWindow.Min(p => p.Position);
//                        int maxPos = activeWindow.Max(p => p.Position);

//                        if (maxPos - minPos <= proximity)
//                        {
//                            var result = results.FirstOrDefault(r => r.FileId == Id);
//                            if (result == null)
//                            {
//                                result = new SearchResult { FileId = Id };
//                                results.Add(result);
//                            }

//                            result.MatchedPostings.Add(activeWindow.ToArray());

//                            for (int i = 0; i < iterators.Count; i++)
//                            {
//                                iterators[i].Dequeue();
//                            }
//                        }
//                        else
//                        {
//                            int minIndex = activeWindow
//                                .Select((p, idx) => (p, idx))
//                                .OrderBy(t => t.p.Position)
//                                .First().idx;

//                            iterators[minIndex].Dequeue();
//                        }
//                    }
//                }
//            }
//            return results;
//        }

//        static Dictionary<string, List<Token>> GetTokens(List<List<string>> termGroups)
//        {
//            var tokens = new Dictionary<string, List<Token>>();

//            using (var indexReader = new IndexReader(true))
//            {
//                foreach (var group in termGroups)
//                {
//                    foreach (var term in group)
//                    {
//                        if (!tokens.ContainsKey(term))
//                            tokens[term] = new List<Token>();

//                        var data = indexReader.Get(term);
//                        if (data != null)
//                        {
//                            var deserialized = TokenSerializer.Deserialize(data).ToList();
//                            tokens[term].AddRange(deserialized);
//                        }
//                    }
//                }
//            }

//            return tokens;
//        }

//        static Dictionary<int, Dictionary<string, Postings[]>> GetFileGroups(
//            List<List<string>> termGroups,
//            Dictionary<string, List<Token>> tokens)
//        {
//            var fileGroups = new Dictionary<int, Dictionary<string, Postings[]>>();

//            foreach (var group in termGroups)
//            {
//                foreach (var term in group)
//                {
//                    foreach (var token in tokens[term])
//                    {
//                        if (!fileGroups.ContainsKey(token.ID))
//                            fileGroups[token.ID] = new Dictionary<string, Postings[]>();

//                        if (!fileGroups[token.ID].ContainsKey(term))
//                            fileGroups[token.ID][term] = token.Postings.OrderBy(p => p.Position).ToArray();
//                    }
//                }
//            }

//            return fileGroups;
//        }
//    }
//}
