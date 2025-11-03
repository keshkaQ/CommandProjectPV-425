using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;

namespace TaskParallelLibrary.Tests;

public class MaxFrequencyOfElements
{
    private int[] _array;
    private readonly int _size;

    public MaxFrequencyOfElements(int size)
    {
        _size = size;
    }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _array = Enumerable.Range(0, _size).Select(_ => random.Next(25000)).ToArray();
    }

    //-------------------------------------------------------------------------------------
    //                                                                            Array_For
    //-------------------------------------------------------------------------------------
    [Benchmark(Baseline = true)]
    public int Array_For()
    {
        var counts = new Dictionary<int, int>();
        foreach (int val in _array)
        {
            counts.TryGetValue(val, out int currentCount);
            counts[val] = currentCount + 1;
        }

        int maxFreq = 0;
        foreach (var freq in counts.Values)
            if (freq > maxFreq)
                maxFreq = freq;

        return maxFreq;
    }

    //-------------------------------------------------------------------------------------
    //                                                                          Array_PLINQ
    //-------------------------------------------------------------------------------------
    [Benchmark]
    public int Array_PLINQ()
    {
        return _array
            .AsParallel()
            .GroupBy(x => x)
            .Max(g => g.Count());
    }

    //-------------------------------------------------------------------------------------
    //                                                                         Parallel_For
    //-------------------------------------------------------------------------------------
    [Benchmark]
    public int Parallel_For()
    {
        var globalCounts = new Dictionary<int, int>();
        var lockObj = new object();

        Parallel.For(0, _array.Length,
            // localInit
            () => new Dictionary<int, int>(),
            // body
            (i, state, localCounts) =>
            {
                int val = _array[i];
                localCounts.TryGetValue(val, out int currentCount);
                localCounts[val] = currentCount + 1;
                return localCounts;
            },
            // localFinally
            localCounts =>
            {
                lock (lockObj)
                {
                    foreach (var kvp in localCounts)
                    {
                        globalCounts.TryGetValue(kvp.Key, out int currentCount);
                        globalCounts[kvp.Key] = currentCount + kvp.Value;
                    }
                }
            });

        return globalCounts.Values.Max();
    }

    //-------------------------------------------------------------------------------------
    //                                                                 Parallel_Partitioner
    //-------------------------------------------------------------------------------------
    [Benchmark]
    public int Parallel_Partitioner()
    {
        var globalCounts = new Dictionary<int, int>();
        var lockObj = new object();

        Parallel.ForEach(Partitioner.Create(0, _array.Length),
            () => new Dictionary<int, int>(),
            (range, state, localCounts) =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    int val = _array[i];
                    localCounts.TryGetValue(val, out int currentCount);
                    localCounts[val] = currentCount + 1;
                }
                return localCounts;
            },
            localCounts =>
            {
                lock (lockObj)
                {
                    foreach (var kvp in localCounts)
                    {
                        globalCounts.TryGetValue(kvp.Key, out int currentCount);
                        globalCounts[kvp.Key] = currentCount + kvp.Value;
                    }
                }
            });

        return globalCounts.Values.Max();
    }

    //-------------------------------------------------------------------------------------
    //                                                                      Parallel_Invoke
    //-------------------------------------------------------------------------------------
    [Benchmark]
    public int Parallel_Invoke()
    {
        int cores = Environment.ProcessorCount;
        int chunk = (_array.Length + cores - 1) / cores;
        var globalCounts = new Dictionary<int, int>();
        var lockObj = new object();

        var actions = new Action[cores];

        for (int c = 0; c < cores; c++)
        {
            int start = c * chunk;
            int end = Math.Min(start + chunk, _array.Length);

            actions[c] = () =>
            {
                var localCounts = new Dictionary<int, int>();
                for (int i = start; i < end; i++)
                {
                    int val = _array[i];
                    localCounts.TryGetValue(val, out int currentCount);
                    localCounts[val] = currentCount + 1;
                }

                lock (lockObj)
                {
                    foreach (var kvp in localCounts)
                    {
                        globalCounts.TryGetValue(kvp.Key, out int currentCount);
                        globalCounts[kvp.Key] = currentCount + kvp.Value;
                    }
                }
            };
        }

        Parallel.Invoke(actions);
        return globalCounts.Values.Max();
    }

    //-------------------------------------------------------------------------------------
    //                                                                            Tasks_Run
    //-------------------------------------------------------------------------------------
    [Benchmark]
    public int Tasks_Run()
    {
        int processorCount = Environment.ProcessorCount;
        int chunkSize = (_array.Length + processorCount - 1) / processorCount;

        var tasks = new Task<Dictionary<int, int>>[processorCount];

        for (int i = 0; i < processorCount; i++)
        {
            int start = i * chunkSize;
            int end = Math.Min(start + chunkSize, _array.Length);

            tasks[i] = Task.Run(() =>
            {
                var localCounts = new Dictionary<int, int>();
                for (int j = start; j < end; j++)
                {
                    int val = _array[j];
                    localCounts.TryGetValue(val, out int currentCount);
                    localCounts[val] = currentCount + 1;
                }
                return localCounts;
            });
        }

        Task.WaitAll(tasks);

        var globalCounts = new Dictionary<int, int>();
        foreach (var local in tasks)
        {
            foreach (var kvp in local.Result)
            {
                globalCounts.TryGetValue(kvp.Key, out int currentCount);
                globalCounts[kvp.Key] = currentCount + kvp.Value;
            }
        }

        return globalCounts.Values.Max();
    }
}
