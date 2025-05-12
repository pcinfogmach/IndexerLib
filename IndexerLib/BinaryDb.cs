using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IndexerLib
{
    public class IndexEntry
    {
        public long Offset { get; set; }
        public int Length { get; set; }
    }
    public class BinaryDbWriter : IDisposable
    {
        private const ushort MagicMarker = 0xCAFE; // 16-bit marker
        private readonly FileStream stream;
        private readonly Dictionary<string, IndexEntry> index = new Dictionary<string, IndexEntry>();
        private long currentOffset = 0;
        private SHA256 sha256;

        public BinaryDbWriter(string path)
        {
            stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            sha256 = SHA256.Create();
        }

        public void AddRecord(string key, byte[] data)
        {
            var keyHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(key)));

            if (!index.ContainsKey(keyHash))
            {
                stream.Write(data, 0, data.Length);
                index[keyHash] = new IndexEntry { Offset = currentOffset, Length = data.Length };
                currentOffset += data.Length;
            }
            else
            {
                throw new InvalidOperationException("Index already contains key!");
            }

        }

        public void Dispose()
        {
            try
            {
                // Write index to end of stream
                long indexStart = stream.Position;
                using (var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false))
                {
                    bw.Write(index.Count);
                    foreach (var kvp in index)
                    {
                        bw.Write(kvp.Key); // hashed key
                        bw.Write(kvp.Value.Offset);
                        bw.Write(kvp.Value.Length);
                    }

                    long indexLength = stream.Position - indexStart;

                    // Combine magic number and index length into a single 8-byte footer
                    ulong footer = ((ulong)MagicMarker << 48) | (ulong)indexLength;
                    bw.Write(footer);
                }

                stream.Dispose();
                sha256.Dispose();
            }
            catch { }
        }
    }

    public class BinaryDbReader
    {
        private const ushort MagicMarker = 0xCAFE;
        private readonly Dictionary<string, IndexEntry> index = new Dictionary<string, IndexEntry>();
        private readonly FileStream stream;

        public BinaryDbReader(string path)
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            LoadIndex();
        }

        private void LoadIndex()
        {
            using (var br = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                // Read footer (last 8 bytes)
                stream.Seek(-8, SeekOrigin.End);
                ulong footer = br.ReadUInt64();
                ushort magic = (ushort)(footer >> 48);
                ulong indexLength = footer & 0xFFFFFFFFFFFF;

                if (magic != MagicMarker)
                    throw new InvalidDataException("Invalid file format (magic mismatch)");

                // Seek to beginning of index
                long indexStart = stream.Length - 8 - (long)indexLength;
                stream.Seek(indexStart, SeekOrigin.Begin);

                int entryCount = br.ReadInt32();
                for (int i = 0; i < entryCount; i++)
                {
                    string keyHash = br.ReadString();
                    long offset = br.ReadInt64();
                    int length = br.ReadInt32();
                    index[keyHash] = new IndexEntry { Offset = offset, Length = length };
                }
            }
        }

        public byte[] GetRecord(string key)
        {
            using (var sha256 = SHA256.Create())
            {
                string keyHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(key)));
                if (!index.TryGetValue(keyHash, out var entry))
                    throw new KeyNotFoundException("Key not found in index");

                byte[] buffer = new byte[entry.Length];
                stream.Seek(entry.Offset, SeekOrigin.Begin);
                stream.Read(buffer, 0, entry.Length);
                return buffer;
            }
        }

        public void Close() => stream.Dispose();
    }

}
