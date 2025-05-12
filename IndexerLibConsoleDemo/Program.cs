using IndexerLib.IndexManager;
using IndexerLib.IndexManger;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IndexerLibConsoleDemo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.GetEncoding("Windows-1255");

            string indexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index");
            string toratEmet = "C:\\Users\\Admin\\Desktop\\תורת אמת";
            var files = Directory.GetFiles(toratEmet, "*", SearchOption.AllDirectories);
            FileIndexer.CreateIndex(files);

            var results = Search.UnorderedProximitySearch("כי ביצחק", proximity: 3);
            var generator = new SnippetGenerator();

            foreach (var res in results)
            {
                var snippets = generator.GenerateSnippets(res);
                foreach (var s in snippets)
                {
                    Console.WriteLine($"File: {res.FilePath.ReverseHebrewCharacters()}\nSnippet: {s.ReverseHebrewCharacters()}\n");
                }
            }
        }
    }

    public static class HebrewStringFixer
    {
        public static string ReverseHebrewCharacters(this string input)
        {
            Regex hebrewRegex = new Regex(@"\p{IsHebrew}+(\W+\p{IsHebrew}+)?");
            return hebrewRegex.Replace(input, match => new string(match.Value.Reverse().ToArray()));
        }
    }
}

