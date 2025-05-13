using System.Collections.Generic;
using System.Linq;

namespace IndexerLib.Helpers
{
    class OcrFixer
    {
        public static List<Token> FixWord(string word, HashSet<string> dict, int maxDistance = 1)
        {
            var memo = new Dictionary<int, List<List<(string word, int start, int length)>>>();
            var segments = Segment(word, 0, dict, maxDistance, memo);

            if (segments.Count == 0) return new List<Token>();

            // Pick segmentation with lowest total Levenshtein distance
            var bestSeg = segments.OrderBy(seg =>
                seg.Sum(part => Levenshtein.Distance(word.Substring(part.start, part.length), part.word))
            ).First();

            var tokens = new List<Token>();
            int pos = 0;
            foreach (var seg in bestSeg)
            {
                tokens.Add(new Token
                {
                    ID = seg.word,
                    Postings = new List<Postings>
            {
                new Postings
                {
                    Position = pos++,
                    StartIndex = seg.start,
                    Length = seg.length
                }
            }
                });
            }
            return tokens;
        }


        static List<List<(string word, int start, int length)>> Segment(string s, int start, HashSet<string> dict, int maxDist, Dictionary<int, List<List<(string, int, int)>>> memo)
        {
            if (memo.ContainsKey(start)) return memo[start];
            var res = new List<List<(string, int, int)>>();

            for (int end = start + 1; end <= s.Length; end++)
            {
                var part = s.Substring(start, end - start);
                foreach (var dictWord in dict)
                {
                    if (Levenshtein.Distance(part, dictWord) <= maxDist)
                    {
                        if (end == s.Length)
                            res.Add(new List<(string, int, int)> { (dictWord, start, end - start) });
                        else
                        {
                            var suffixes = Segment(s, end, dict, maxDist, memo);
                            foreach (var suf in suffixes)
                            {
                                var current = new List<(string, int, int)> { (dictWord, start, end - start) };
                                current.AddRange(suf);
                                res.Add(current);
                            }
                        }
                    }
                }
            }
            memo[start] = res;
            return res;
        }
    }
}
