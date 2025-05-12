using IndexerLib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IndexerLib.Index
{
    public class IndexWriter : IndexBase, IDisposable
    {
        private FileStream fileStream;
        BinaryWriter writer;

        private long currentOffset = 0;
        Dictionary<byte[], IndexKey> Keys = new Dictionary<byte[], IndexKey>();

        public IndexWriter(string fileName = "db", string directoryName = "Index") : base(fileName, directoryName)
        {
            while(File.Exists(FilePath))
                FilePath = Path.Combine(DirectoryPath, Path.GetFileNameWithoutExtension(FilePath) + "+.index");

            fileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            writer = new BinaryWriter(fileStream, Encoding.UTF8, leaveOpen: true);
        }

        public void Put(byte[] data, byte[] hash = null, string key = "")
        {
            if (hash == null && string.IsNullOrWhiteSpace(key))
                return;

            if (hash == null)
                hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));

            if (!string.IsNullOrWhiteSpace(key))
                Words.Add(key);

            writer.Write(data, 0, data.Length);

            Keys[hash] = new IndexKey
            {
                Hash = hash,
                Offset = currentOffset,
                Length = data.Length
            };

            currentOffset += data.Length;
        }

        public override void Dispose()
        {
            AppendKeys(); // must happen before disposal

            File.WriteAllLines(WordsFile, Words.OrderBy(k => k));

            writer.Flush();  
            writer.Dispose(); 
            fileStream.Dispose();

            base.Dispose();
        }

        void AppendKeys()
        {
            if (fileStream == null)
                return;

            try
            {
                long indexStart = fileStream.Position;

                // Sort entries by hash
                var sortedIndex = Keys.OrderBy(kvp => kvp.Key, new ByteArrayComparer());

                writer.Write(Keys.Count);
                foreach (var indexKey in sortedIndex)
                {
                    writer.Write(indexKey.Key); // 32-byte hash
                    writer.Write(indexKey.Value.Offset); // int64
                    writer.Write(indexKey.Value.Length); // int32
                }

                long indexLength = fileStream.Position - indexStart;
                ulong footer = ((ulong)MagicMarker << 48) | (ulong)indexLength;
                writer.Write(footer);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
    }
}
