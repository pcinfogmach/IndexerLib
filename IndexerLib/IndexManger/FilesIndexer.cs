using IndexerLib.Tokens;
using System;
using System.IO;
using System.Timers;

namespace IndexerLib.IndexManger
{
    public static class FileIndexer
    {
        public static void CreateIndex(string[] files)
        {
            var indexStart = DateTime.Now;
            int fileCount = files.Length;
            int currentIndex = -1;

            Timer progressTimer = new Timer(2000); // 1-second interval
            progressTimer.Elapsed += (sender, e) =>
                Console.WriteLine($"File Progress: {currentIndex} / {fileCount}");

            progressTimer.Start();

            using (var wal = new WAL())
            {
                foreach (var file in files)
                {
                    currentIndex++;
                    try
                    {

                        string content = File.ReadAllText(file);
                        var tokens = Tokenizer.Tokenize(content, file);

                        foreach (var token in tokens)
                            wal.Log(token.Key, token.Value);
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
            }

            progressTimer.Stop();
            progressTimer.Dispose();

            Console.WriteLine("Merge Start...");
            IndexMerger.Merge();

            Console.WriteLine($"Indexing complete! start time: {indexStart} end time: {DateTime.Now} total time: {DateTime.Now - indexStart}");
        }
    }
}
