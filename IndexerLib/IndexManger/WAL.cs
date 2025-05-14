using IndexerLib.Tokens;
using IndexerLib.Index;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace IndexerLib.IndexManger
{
    public class WAL : IDisposable
    {
        private ConcurrentQueue<string> _mergeQueue = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<KeyValuePair<string, byte[]>> _logQueue = new ConcurrentQueue<KeyValuePair<string, byte[]>>();
        
        private Task _backgroundMergeTask;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        object _mergeLock = new object();
        int _threshHold;

        public WAL(float memoryUsagePercent = 20)  
        {

            _threshHold = CalculateDynamicThreshold(memoryUsagePercent);

            using (var index = new IndexBase(false))
            {
                var files = index.GetAllIndexFiles();
                foreach (var file in files)
                    _mergeQueue.Enqueue(file);
            }
            
            //_backgroundMergeTask = Task.Run(() => BackgroundMerge(_cts.Token));
        }

        private int CalculateDynamicThreshold(float percent)
{
    try
    {
        using (var pc = new PerformanceCounter("Memory", "Available MBytes"))
        {
            float availableMb = pc.NextValue();
            float targetUsageMb = availableMb * (percent / 100f);
            //int bytesPerItem = 1000; // Configurable assumption
            return (int)(targetUsageMb * 1_000);
        }
    }
    catch (Exception ex)
    {
        // Log the exception for debugging purposes
        Console.WriteLine($"Failed to calculate threshold: {ex.Message}");
        // Fallback value based on 800,000 entries
        return 1_000_000;
    }
}


        public void Log(string key, Token token)
        {
            if (_logQueue.Count > _threshHold)
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

            System.Timers.Timer progressTimer = new System.Timers.Timer(1000); // 1-second interval
            progressTimer.Elapsed += (sender, e) =>
                Console.WriteLine($"Flush Progress: {flushIndex} / {flushCount}");

            progressTimer.Start();
            
            string indexPath;
            using (var writer = new IndexWriter(true))
            {
                foreach (var entry in groupedData)
                {
                    flushIndex++;
                    var combined = entry.Value.SelectMany(b => b).ToArray(); 
                    writer.Put(data: combined, key: entry.Key);
                }
                indexPath = writer.FilePath;
            }

            if (!string.IsNullOrEmpty(indexPath))
                _mergeQueue.Enqueue(indexPath);

            progressTimer.Stop();
            progressTimer.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine("Flush done");
        }

        public void Dispose()
        {
            Flush();

            _cts.Cancel();
            MergeRemaining();
        }

        async Task BackgroundMerge(CancellationToken t)
        {
            while (!t.IsCancellationRequested)
            {
                if (_mergeQueue.Count < 2)
                {
                    await Task.Delay(100, t);
                    continue;
                }
                

                lock (_mergeLock)
                {
                    List<string> files = new List<string>();
                    while (_mergeQueue.TryDequeue(out var file))
                        files.Add(file);

                    string mergedPath = IndexMerger.Merge(files);
                    _mergeQueue.Enqueue(mergedPath);
                }
            }
        }

        void MergeRemaining()
        {
            while (!Monitor.TryEnter(_mergeLock))
                Thread.Sleep(10);

            List<string> files;
            using (var index = new IndexBase(false))
                files = index.GetAllIndexFiles().ToList();               

            if (files != null && files.Count > 1) 
                IndexMerger.Merge(files);
        }
    }
}
