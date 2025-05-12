using IndexerLib.IndexManger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IndexerLib.IndexManager
{
    public class SnippetGenerator
    {
        public List<string> GenerateSnippets(SearchResult result, int contextWindow = 30)
        {
            var snippets = new List<string>();

            if (!File.Exists(result.FilePath))
                return snippets;

            string content = File.ReadAllText(result.FilePath);

            foreach (var match in result.MatchedPostings)
            {
                // Get range of match
                int start = match.Min(p => p.StartIndex);
                int end = match.Max(p => p.StartIndex + p.Length);

                // Expand window
                int snippetStart = Math.Max(0, start - contextWindow);
                int snippetEnd = Math.Min(content.Length, end + contextWindow);
                int snippetLength = snippetEnd - snippetStart;

                string snippet = content.Substring(snippetStart, snippetLength);

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

                snippets.Add(snippet);
            }

            return snippets;
        }
    }

}
