using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IndexerLib.IndexManger
{
    public static class WordListManager
    {

        static readonly string _wordsFile = Path.Combine(GetIndexFolder(), "Keys.txt");
        
        public static bool Exists => File.Exists(_wordsFile);
        
        public static IEnumerable<string> Words =>
            File.Exists(_wordsFile) ? File.ReadLines(_wordsFile) : Enumerable.Empty<string>();
       
        public static void AddRange(HashSet<string> newWords)
        {
            foreach (string word in Words)
                newWords.Add(word);

            File.WriteAllLines(_wordsFile, newWords.OrderBy(k => k));
        }

        static string GetIndexFolder()
        {
            string indexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index");
            if(!Directory.Exists(indexPath))
                Directory.CreateDirectory(indexPath);
            return indexPath;
        }

    }
}
