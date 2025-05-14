using IndexerLib.Helpers;
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
        public static List<string> Parse(string input)
        {
            var splitRaw = input.Split('|').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

            if (!WordListManager.Exists)
                return splitRaw; // אם הקובץ לא קיים – פשוט נחזיר את המילים שהוזנו

            var allWords = WordListManager.Words;
            var results = new List<string>();

            foreach (var word in allWords)
            {
                foreach (var rawTerm in splitRaw)
                {
                    if (rawTerm.Contains("*") || rawTerm.Contains("?"))
                    {
                        var pattern = "^" + Regex.Escape(rawTerm)
                            .Replace(@"\*", ".*")
                            .Replace(@"\?", ".") + "$";
                        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

                        if (regex.IsMatch(word))
                            results.Add(word);
                        //results.AddRange(allWords.Where(word => regex.IsMatch(word)));
                    }
                    else if (rawTerm.EndsWith("~"))
                    {
                        if (Levenshtein.Distance(word, rawTerm.TrimEnd('~')) < 2)
                            results.Add(word);

                        //results.AddRange(allWords.Where(word => Levenshtein.Distance(word, term) < 2));
                    }
                    else
                    {
                        results.Add(rawTerm);
                    }
                }
            }

            return results.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

    }
}
