using IndexerLib.IndexManager;
using IndexerLib.IndexManger;
using System;
using System.Collections.Generic;
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
            var startTime = DateTime.Now;
            Console.OutputEncoding = Encoding.GetEncoding("Windows-1255");

            string indexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index");
            //string toratEmet = "C:\\אוצריא\\אוצריא";
            //string toratEmet = "C:\\Users\\Admin\\Desktop\\תורת אמת\\01_תנך\\001_תורה";
            //string toratEmet = "C:\\Users\\Admin\\Desktop\\תורת אמת\\01_תנך";
            string toratEmet = "C:\\Users\\Admin\\Desktop\\תורת אמת";

            var exts = new[] { "*.txt", "*.html" };
            var files = exts.SelectMany(ext => Directory.GetFiles(toratEmet, ext, SearchOption.AllDirectories)).ToArray();
            //FileIndexer.CreateIndex(files);

            var results = Search.GetSnippets("כי~ ביצחק~", proximity: 3);
            foreach (var r in results)
            {
                foreach (var s in r.Snippets)
                    Console.WriteLine($"File: {r.Key.ReverseHebrewCharacters()}\nSnippet:" +
                        $" {s.ReverseHebrewCharacters()}");
            }

            Console.WriteLine("Finished! Total time: " + (DateTime.Now - startTime));
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

