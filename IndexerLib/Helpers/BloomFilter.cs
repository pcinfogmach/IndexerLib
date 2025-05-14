using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public class DynamicBloomFilter
{
    private readonly List<BloomFilter> filters = new List<BloomFilter>();
    private readonly int initialSize;
    private readonly int initialHashCount;
    private readonly double loadFactorThreshold;
    private readonly double growthFactor;

    public DynamicBloomFilter(int initialSize = 8192, int hashCount = 7, double loadThreshold = 0.6, double growth = 2.0)
    {
        this.initialSize = initialSize;
        this.initialHashCount = hashCount;
        this.loadFactorThreshold = loadThreshold;
        this.growthFactor = growth;

        // Add the first Bloom filter
        filters.Add(new BloomFilter(initialSize, hashCount));
    }

    public void Add(string item)
    {
        var last = filters[filters.Count - 1];
        // Check if the current filter exceeds the load factor
        if (last.LoadFactor() > loadFactorThreshold)
        {
            // Create a new Bloom filter with increased size and hash count
            int newSize = (int)(last.Size * growthFactor);
            int newHashCount = last.HashCount + 2; // Reduce false positives as we grow
            filters.Add(new BloomFilter(newSize, newHashCount));
            last = filters[filters.Count - 1];
        }

        last.Add(item);
    }

    public bool Contains(string item)
    {
        // Check all filters for the item
        foreach (var filter in filters)
        {
            if (filter.Contains(item))
                return true;
        }
        return false;
    }

    public void Save(string path)
    {
        using (var fs = new FileStream(path, FileMode.Create))
        using (var gz = new GZipStream(fs, CompressionMode.Compress))
        using (var bw = new BinaryWriter(gz))
        {
            bw.Write(filters.Count);
            foreach (var filter in filters)
                filter.Save(bw);
        }
    }

    public static DynamicBloomFilter Load(string path)
    {
        using (var fs = new FileStream(path, FileMode.Open))
        using (var gz = new GZipStream(fs, CompressionMode.Decompress))
        using (var br = new BinaryReader(gz))
        {
            int count = br.ReadInt32();
            var dbf = new DynamicBloomFilter();
            dbf.filters.Clear(); // Remove default
            for (int i = 0; i < count; i++)
            {
                dbf.filters.Add(BloomFilter.Load(br));
            }
            return dbf;
        }
    }
}

// BloomFilter Class Implementation
public class BloomFilter
{
    private readonly int[] bits;
    private readonly int size;
    private readonly int hashCount;
    private int itemCount;

    public int Size => size;
    public int HashCount => hashCount;

    public BloomFilter(int size, int hashCount)
    {
        this.size = size;
        this.hashCount = hashCount;
        bits = new int[size];
        itemCount = 0;
    }

    public void Add(string item)
    {
        foreach (var hash in GetHashes(item))
        {
            bits[hash] = 1;
        }
        itemCount++;
    }

    public bool Contains(string item)
    {
        foreach (var hash in GetHashes(item))
        {
            if (bits[hash] == 0)
                return false;
        }
        return true;
    }

    public double LoadFactor()
    {
        return (double)itemCount / size;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(size);
        writer.Write(hashCount);
        writer.Write(itemCount);
        foreach (var bit in bits)
        {
            writer.Write(bit);
        }
    }

    public static BloomFilter Load(BinaryReader reader)
    {
        int size = reader.ReadInt32();
        int hashCount = reader.ReadInt32();
        int itemCount = reader.ReadInt32();
        var filter = new BloomFilter(size, hashCount)
        {
            itemCount = itemCount
        };
        for (int i = 0; i < size; i++)
        {
            filter.bits[i] = reader.ReadInt32();
        }
        return filter;
    }

    private IEnumerable<int> GetHashes(string item)
    {
        var hash1 = item.GetHashCode();
        var hash2 = (hash1 >> 16) ^ hash1;

        for (int i = 0; i < hashCount; i++)
        {
            yield return Math.Abs((hash1 + i * hash2) % size);
        }
    }
}


//class Program
//{
//    static void Main(string[] args)
//    {
//        var dynamicBloomFilter = new DynamicBloomFilter();

//        // Add items
//        dynamicBloomFilter.Add("apple");
//        dynamicBloomFilter.Add("banana");
//        dynamicBloomFilter.Add("cherry");

//        // Check items
//        Console.WriteLine(dynamicBloomFilter.Contains("apple")); // True
//        Console.WriteLine(dynamicBloomFilter.Contains("grape")); // False

//        // Save to disk
//        dynamicBloomFilter.Save("bloom_filter.db");

//        // Load from disk
//        var loadedFilter = DynamicBloomFilter.Load("bloom_filter.db");
//        Console.WriteLine(loadedFilter.Contains("apple")); // True
//    }
//}
