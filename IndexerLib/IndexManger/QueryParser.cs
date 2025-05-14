using IndexerLib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IndexerLib.IndexManger
{
    public static class QueryParser
    {
        public static List<List<string>> Parse(string input)
        {
            input = Regex.Replace(input, @"\s*\|\s*", "|");
            input = Regex.Replace(input, @"\s*~", "~");

            var terms = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var termGroups = terms.Select(t => t.Split('|').ToList()).ToList();

            if (!WordListManager.Exists)
                return termGroups;


            var result = new List<List<string>>(termGroups.Count);
            for (int i = 0; i < termGroups.Count; i++)
                result.Add(new List<string>());


            foreach (var word in WordListManager.Words)
                for (int i = 0; i < termGroups.Count; i++)
                {
                    foreach (var rawTerm in termGroups[i])
                    {
                        if (rawTerm.Contains("*") || rawTerm.Contains("?"))
                        {
                            var pattern = "^" + Regex.Escape(rawTerm).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
                            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                            if (regex.IsMatch(word))
                                result[i].Add(word);
                        }
                        else if (rawTerm.EndsWith("~"))
                        {
                            var baseTerm = rawTerm.TrimEnd('~');
                            if (Levenshtein.Distance(word, baseTerm) < 2)
                                result[i].Add(word);
                        }
                        else if (string.Equals(word, rawTerm, StringComparison.OrdinalIgnoreCase))
                        {
                            result[i].Add(word);
                        }
                    }
                }

            return result;
        }
    }
}
