using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IndexerLib.IndexManger
{
    public static class QueryParser
    {
        public static List<string> ParseTerm(string input)
        {
            string wordsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index", "Keys.txt");
            var allWords = File.ReadLines(wordsFile).ToList();

            var results = new List<string>();

            var splitRaw = input.Split('|');
            foreach (var rawTerm in splitRaw)
            {
                if (rawTerm.Contains("*") || rawTerm.Contains("?"))
                {
                    // Convert to Regex
                    var pattern = "^" + Regex.Escape(rawTerm)
                        .Replace(@"\*", ".*")
                        .Replace(@"\?", ".") + "$";
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);

                    results = allWords.Where(word => regex.IsMatch(word)).ToList();
                }
                else if (rawTerm.EndsWith("~"))
                {
                    var term = rawTerm.TrimEnd('~');

                    results.AddRange(allWords
                        .Where(word => Levenshtein.Distance(word, term) < 2)
                        .ToList());
                }
                else
                {
                    results.Add(rawTerm); // Exact term
                }
            }
            return results;
        }
    }
}
