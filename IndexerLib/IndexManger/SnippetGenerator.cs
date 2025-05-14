using IndexerLib.IndexManger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IndexerLib.IndexManager
{
    public class SnippetCollection
    {
        public string Key { get; set; }
        public List<string> Snippets { get; set; } = new List<string>();
    }

    public static class SnippetGenerator
    {
        public static SnippetCollection Generate(IdStore idStore, IGrouping<int, SearchResult> results, int contextWindow = 30)
        {
            SnippetCollection snippetsCol = new SnippetCollection { Key = idStore.GetNameById(results.Key )};

            if (!File.Exists(snippetsCol.Key))
                return snippetsCol;

            string content = File.ReadAllText(snippetsCol.Key);
            foreach (var item in results)
            {
                foreach (var match in item.MatchedPostings)
                {
                    // Get range of match
                    int start = match.Min(p => p.StartIndex);
                    int end = match.Max(p => p.StartIndex + p.Length);

                    // Expand window
                    int snippetStart = Math.Max(0, start - contextWindow);
                    int snippetEnd = Math.Min(content.Length, end + contextWindow);
                    int snippetLength = snippetEnd - snippetStart;

                    string snippet = content.Substring(snippetStart, snippetLength);
                    snippet = Regex.Replace(snippet, @"(?:<[^\s]+>)|(?:<[^\s]+)|(?:[^\s]+>)", "");
                    // Adjust offsets for highlight
                    var highlights = match
                        .OrderBy(p => p.StartIndex)
                        .Select(p => new
                        {
                            RelativeStart = p.StartIndex - snippetStart,
                            p.Length
                        })
                        .Where(h => h.RelativeStart >= 0 && h.RelativeStart + h.Length <= snippet.Length)
                        .ToList();

                    // Apply <mark> tags in reverse order to preserve offsets
                    for (int i = highlights.Count - 1; i >= 0; i--)
                    {
                        var h = highlights[i];
                        snippet = snippet.Insert(h.RelativeStart + h.Length, "</mark>");
                        snippet = snippet.Insert(h.RelativeStart, "<mark>");
                    }

                    snippetsCol.Snippets.Add(snippet);
                }
            }

            return snippetsCol;
        }
    }
}
