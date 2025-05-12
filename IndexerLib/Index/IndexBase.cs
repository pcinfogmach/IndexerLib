using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace IndexerLib.Index
{
    public class IndexBase : IDisposable
    {
        protected SHA256 sha256 = SHA256.Create();
        protected const ushort MagicMarker = 0xCAFE;
        public HashSet<string> Words { get; protected set; }
        public string WordsFile { get; protected set; }
        public string FilePath { get; protected set; }
        public string DirectoryPath { get; protected set; }

        public IndexBase(string fileName = "db", string directoryName = "Index") 
        {
            DirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, directoryName);
            FilePath = Path.Combine(DirectoryPath, $"{fileName.Replace(".index", "")}.index");

            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);

            WordsFile = Path.Combine(DirectoryPath, "Keys.txt");

            Words = new HashSet<string>(File.Exists(WordsFile)
             ? File.ReadLines(WordsFile)
             : Enumerable.Empty<string>());
        }

        public IEnumerable<string> GetAllIndexFiles()
        {
            foreach (var indexFile in Directory.GetFiles(DirectoryPath, "*.index"))
                yield return indexFile;
        }

        public virtual void Dispose()
        {
            sha256?.Dispose();
        }
    }
}
