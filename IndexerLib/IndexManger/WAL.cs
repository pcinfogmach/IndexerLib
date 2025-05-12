using IndexerLib.Tokens;
using IndexerLib.Index;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;
using System.Linq;

namespace IndexerLib.IndexManger
{
    public class WAL : IDisposable
    {
        private readonly ConcurrentQueue<KeyValuePair<string, byte[]>> _logQueue = new ConcurrentQueue<KeyValuePair<string, byte[]>>();

        public WAL()  { }

        public void Log(string key, Token token)
        {
            if (_logQueue.Count > 1000000)
                Flush();

            byte[] serialized = TokenSerializer.Serialize(token);
            _logQueue.Enqueue(new KeyValuePair<string, byte[]>(key, serialized));
        }

        public void Flush()
        {
            var groupedData = new Dictionary<string, List<byte[]>>();

            while (_logQueue.TryDequeue(out var item))
            {
                if (!groupedData.ContainsKey(item.Key))
                    groupedData[item.Key] = new List<byte[]>();

                groupedData[item.Key].Add(item.Value); // assuming item.Tokens is byte[]
            }

            if (groupedData.Count == 0)
                return;

            Console.WriteLine("Flushing...");

            int flushCount = groupedData.Count;
            int flushIndex = 0;

            Timer progressTimer = new Timer(1000); // 1-second interval
            progressTimer.Elapsed += (sender, e) =>
                Console.WriteLine($"Flush Progress: {flushIndex} / {flushCount}");

            progressTimer.Start();
            using (var index = new IndexWriter())
                foreach (var entry in groupedData)
                {
                    flushIndex++;
                    var combined = entry.Value.SelectMany(b => b).ToArray(); // if you need all byte[]s as one
                    index.Put(data: combined, key: entry.Key);
                }

            progressTimer.Stop();
            progressTimer.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine("Flush done");
        }

        public void Dispose()
        {
            Flush();
        }
    }
}
