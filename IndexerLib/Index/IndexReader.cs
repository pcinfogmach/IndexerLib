using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IndexerLib.Index
{
    public class IndexReader : IndexBase
    {
        FileStream fileStream;
        BinaryReader reader;
        long indexStart;
        int indexCount;
        IEnumerator<IndexKey> _enumerator;

        public IEnumerator<IndexKey> Enumerator
        {
            get
            {
                if (_enumerator == null)
                    _enumerator = GetAllKeys().GetEnumerator();
                return _enumerator;
            }
        }


        public IndexReader(string fileName = "db", string directoryName = "Index") : base(fileName, directoryName)
        {
            InitializeIndex();
        }

        void InitializeIndex()
        {
            if (!File.Exists(FilePath))
                FilePath = GetAllIndexFiles().FirstOrDefault();
            LoadIndex(FilePath);
        }

        void LoadIndex(string path)
        {
            if (!File.Exists(path))
                return;
                         
            reader?.Dispose();
            fileStream?.Dispose();  
            
            fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            reader = new BinaryReader(fileStream, Encoding.UTF8, leaveOpen: true);
            LoadIndexMetadata();
        }

        public byte[] Get(string key = "", byte[] hash = null)
        {
            if (string.IsNullOrEmpty(key) && hash == null)
                return null;

            if (hash == null)
                hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));

            var entry = BinarySearchIndex(hash);
            if (entry == null)
                return null;

            var block = ReadBlock(entry);
            if (block == null)
                return null;

            return block;
        }

        void LoadIndexMetadata()
        {
            if (fileStream.Length < 8)
                return;

            fileStream.Seek(-8, SeekOrigin.End);
            ulong footer = new BinaryReader(fileStream).ReadUInt64();

            if ((ushort)(footer >> 48) != MagicMarker)
                throw new InvalidDataException("Invalid footer/magic marker");

            long indexLength = (long)(footer & 0xFFFFFFFFFFFF);
            indexStart = fileStream.Length - 8 - indexLength;

            fileStream.Seek(indexStart, SeekOrigin.Begin);
            indexCount = reader.ReadInt32();
        }

        public IEnumerable<IndexKey> GetAllKeys()
        {
            if (fileStream.Length < 8)
                yield return null;

            fileStream.Seek(indexStart, SeekOrigin.Begin);
            int keysCount = reader.ReadInt32();
            for (int i = 0; i < keysCount; i++)
            {
                yield return new IndexKey
                {
                    Hash = reader.ReadBytes(32),
                    Offset = reader.ReadInt64(),
                    Length = reader.ReadInt32()
                };
            }
        }

        IndexKey BinarySearchIndex(byte[] targetHash)
        {
            int low = 0, high = indexCount - 1;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                var entry = ReadIndexEntry(mid);
                int cmp = CompareHash(entry.Hash, targetHash);

                if (cmp == 0) return entry;
                if (cmp < 0) low = mid + 1;
                else high = mid - 1;
            }

            return null;
        }

        IndexKey ReadIndexEntry(int index)
        {
            fileStream.Seek(indexStart, SeekOrigin.Begin);

            int count = reader.ReadInt32();
            reader.BaseStream.Seek(index * 44, SeekOrigin.Current);

            byte[] hash = reader.ReadBytes(32);
            long offset = reader.ReadInt64();
            int length = reader.ReadInt32();

            return new IndexKey { Hash = hash, Offset = offset, Length = length };
        }

        static int CompareHash(byte[] a, byte[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                int diff = a[i] - b[i];
                if (diff != 0) return diff;
            }
            return 0;
        }

        public byte[] ReadBlock(IndexKey entry)
        {
            var prevPos = fileStream.Position;
            fileStream.Seek(entry.Offset, SeekOrigin.Begin);
            byte[] buffer = new byte[entry.Length];
            reader.Read(buffer, 0, buffer.Length);
            fileStream.Position = prevPos;
            return buffer;
        }    

        public override void Dispose()
        {
            reader?.Dispose();
            fileStream?.Dispose();
            base.Dispose();
        }
    }
}


//public Dictionary<string, byte[]> GetAll(string[] keys)
//{
//    Dictionary<string, List<byte[]>> blocks = new Dictionary<string, List<byte[]>>();
//    foreach (var key in keys)
//        blocks[key] = new List<byte[]>();

//    foreach (var indexFile in GetAllIndexFiles())
//    {
//        LoadIndex(indexFile);
//        foreach (var key in keys)
//        {
//            var block = Get(key);
//            if (block != null)
//            {
//                blocks[key].Add(block);
//            }
//        }
//    }

//    var results = new Dictionary<string, byte[]>();
//    foreach (var block in blocks)
//    {
//        int totalLength = block.Value.Sum(b => b.Length);
//        var result = new byte[totalLength];
//        int offset = 0;

//        foreach (var entry in block.Value)
//        {
//            Buffer.BlockCopy(entry, 0, result, offset, entry.Length);
//            offset += entry.Length;
//        }

//        results[block.Key] = result;
//    }

//    InitializeIndex();
//    return results;
//}
