using IndexerLib.Helpers;
using IndexerLib.Index;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IndexerLib.IndexManger
{
    public static class IndexMerger
    {
        public static void Merge()
        {
            var writer = new IndexWriter("merged");

            var indexReaders = new List<IndexReader>();
            var files = writer.GetAllIndexFiles().ToList();
            files.Remove(writer.FilePath);

            if (files.Count < 2)
            {
                writer.Dispose();
                File.Delete(writer.FilePath);
                return;
            }

            foreach (var file in files)
            {
                if (file == writer.FilePath)
                    continue;

                var newReader = new IndexReader(file);
                if (newReader.Enumerator.MoveNext())
                    indexReaders.Add(newReader);
            }

            ReadAndMerge(indexReaders, writer);

            foreach (var indexReader in indexReaders)
                indexReader.Dispose();

            foreach (var file in files)
                if (File.Exists(file) && file != writer.FilePath)
                    File.Delete(file);

            writer.Dispose();
        }

        static void ReadAndMerge(List<IndexReader> indexReaders, IndexWriter writer)
        {
            var comparer = new ByteArrayComparer();

            // Preload enumerators
            var activeReaders = new List<IndexReader>();
            foreach (var reader in indexReaders)
            {
                if (reader.Enumerator.MoveNext())
                    activeReaders.Add(reader);
            }

            while (activeReaders.Count > 0)
            {
                // Find the smallest hash
                var minEntry = activeReaders
                    .Where(e => e.Enumerator.Current != null)
                    .OrderBy(e => e.Enumerator.Current.Hash, comparer)
                    .First();

                var currentHash = minEntry.Enumerator.Current.Hash;

                // Collect all readers with the same hash
                var matches = activeReaders
                   .Where(e => comparer.Compare(e.Enumerator.Current.Hash, currentHash) == 0)
                   .ToList();

                // Merge and write the block
                var merged = MergeBlocks(matches.Select(m => m));
                writer.Put(merged, currentHash);

                // Advance all matched enumerators and remove finished ones
                var stillActive = new List<IndexReader>();

                foreach (var reader in activeReaders)
                {
                    if (comparer.Compare(reader.Enumerator.Current.Hash, currentHash) == 0)
                    {
                        if (reader.Enumerator.MoveNext())
                            stillActive.Add(reader);
                    }
                    else
                    {
                        stillActive.Add(reader);
                    }
                }

                activeReaders = stillActive;
            }
        }

        static byte[] MergeBlocks(IEnumerable<IndexReader> indexReaders)
        {
            var uniqueBlocks = new HashSet<byte[]>(new ByteArrayEqualityComparer());
            var mergedBlocks = new List<byte[]>();

            foreach (var index in indexReaders)
            {
                var block = index.ReadBlock(index.Enumerator.Current);
                if (block == null) continue;

                if (uniqueBlocks.Add(block))
                    mergedBlocks.Add(block);
            }

            int totalLength = mergedBlocks.Sum(b => b.Length);
            var merged = new byte[totalLength];
            int offset = 0;

            foreach (var block in mergedBlocks)
            {
                Buffer.BlockCopy(block, 0, merged, offset, block.Length);
                offset += block.Length;
            }

            return merged;
        }
    }
}
