using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace TestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Define your strings
            string string1 = "First String";
            string string2 = "Second String";
            string string3 = "Third String";

            // Create a memory stream to hold the compressed data
            using (var ms = new MemoryStream())
            using (var zip = new GZipStream(ms, CompressionLevel.Optimal, true)) // Create a GZipStream
            using (var writer = new BinaryWriter(zip, Encoding.UTF8)) // Use BinaryWriter for binary data writing
            {
                // List to store the length and offset of each string
                List<(long offset, int length)> stringMetadata = new List<(long, int)>();

                // Write each string to the compressed stream and record its offset and length
                foreach (var str in new[] { string1, string2, string3 })
                {
                    long startOffset = zip.BaseStream.Position;

                    // Convert the string to a byte array and write it to the stream
                    byte[] bytes = Encoding.UTF8.GetBytes(str);
                    writer.Write(bytes);

                    // Record the offset and length of the string
                    stringMetadata.Add((startOffset, bytes.Length));
                }

                // After writing strings, write an index of lengths and offsets at the end of the stream
                long indexStartOffset = zip.BaseStream.Position;

                // Write the number of strings
                writer.Write(stringMetadata.Count);

                // Write the metadata (offset, length) for each string
                foreach (var metadata in stringMetadata)
                {
                    writer.Write(metadata.offset); // Write offset
                    writer.Write(metadata.length); // Write length
                }

                // Optionally, you can also write the compressed data to a file
                File.WriteAllBytes("compressedData.gz", ms.ToArray());
            }

            Console.WriteLine("Data has been written to 'compressedData.gz'.");
        }
    }
}
