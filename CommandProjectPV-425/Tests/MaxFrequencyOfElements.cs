using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace CommandProjectPV_425.Tests
{
    public class MaxFrequencyOfElements
    {
        private int[] _array;
        private readonly int maxValue = 25000;
        public static int Size { get; set; } = 1000000;

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            _array = new int[Size];
            for (int i = 0; i < Size; i++)
            {
                _array[i] = random.Next(maxValue);
            }
        }

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

        [Benchmark]
        public int Array_PLINQ()
        {
            return _array
                .AsParallel()
                .GroupBy(x => x)
                .Max(g => g.Count());
        }


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

        [Benchmark]
        public unsafe int Array_Unsafe()
        {
            var counts = new Dictionary<int, int>(_array.Length / 4);
            int maxFreq = 0;

            fixed (int* ptr = _array)
            {
                int* p = ptr;
                int* end = ptr + _array.Length;

                // Основной цикл (группа по 4)
                while (p + 4 <= end)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int val = p[i];
                        // Использование индексатора (counts[val] = counts[val] + 1)
                        // может быть немного быстрее, чем TryAdd + Get/Set, если ключи уже добавлены.
                        // Однако для данной задачи TryAdd/Set - самый идиоматичный способ.
                        if (counts.TryGetValue(val, out int currentCount))
                        {
                            currentCount++;
                            counts[val] = currentCount;
                        }
                        else
                        {
                            currentCount = 1;
                            counts.Add(val, currentCount);
                        }

                        if (currentCount > maxFreq)
                            maxFreq = currentCount;
                    }
                    p += 4;
                }

                // Хвостовой цикл
                while (p < end)
                {
                    int val = *p;
                    if (counts.TryGetValue(val, out int currentCount))
                    {
                        currentCount++;
                        counts[val] = currentCount;
                    }
                    else
                    {
                        currentCount = 1;
                        counts.Add(val, currentCount);
                    }

                    if (currentCount > maxFreq)
                        maxFreq = currentCount;

                    p++;
                }
            }

            return maxFreq;
        }

        [Benchmark]
        public int Array_SIMD()
        {
            var counts = new Dictionary<int, int>(_array.Length / 4);
            int vectorSize = Vector<int>.Count;
            int length = _array.Length;
            int maxFreq = 0;

            // Использование Vector<T> в этой задаче (подсчет частот) только добавляет
            // накладные расходы, поскольку сам подсчет не векторизуется.
            // Однако, следуя структуре, оставляем обход с помощью Vector<T>
            for (int i = 0; i <= length - vectorSize; i += vectorSize)
            {
                var vector = new Vector<int>(_array, i);
                for (int j = 0; j < vectorSize; j++)
                {
                    int val = vector[j];

                    if (counts.TryGetValue(val, out int currentCount))
                    {
                        currentCount++;
                        counts[val] = currentCount;
                    }
                    else
                    {
                        currentCount = 1;
                        counts.Add(val, currentCount);
                    }

                    if (currentCount > maxFreq)
                        maxFreq = currentCount;
                }
            }

            // Хвостовой цикл
            for (int i = length - length % vectorSize; i < length; i++)
            {
                int val = _array[i];

                if (counts.TryGetValue(val, out int currentCount))
                {
                    currentCount++;
                    counts[val] = currentCount;
                }
                else
                {
                    currentCount = 1;
                    counts.Add(val, currentCount);
                }

                if (currentCount > maxFreq)
                    maxFreq = currentCount;
            }

            return maxFreq;
        }


        //[Benchmark]
        //public unsafe int Array_SIMD_Intrinsics()
        //{
        //    if (!Avx2.IsSupported)
        //        throw new PlatformNotSupportedException("AVX2 not supported on this CPU");

        //    var counts = new Dictionary<int, int>(_array.Length / 4);
        //    int length = _array.Length;
        //    int maxFreq = 0;

        //    fixed (int* ptr = _array)
        //    {
        //        int i = 0;

        //        // Основной цикл (группа по 8 с использованием AVX)
        //        for (; i <= length - 8; i += 8)
        //        {
        //            var vector = Avx.LoadVector256(ptr + i);
        //            for (int j = 0; j < 8; j++)
        //            {
        //                // GetElement(j) извлекает элемент из SIMD-регистра
        //                int val = vector.GetElement(j);

        //                if (counts.TryGetValue(val, out int currentCount))
        //                {
        //                    currentCount++;
        //                    counts[val] = currentCount;
        //                }
        //                else
        //                {
        //                    currentCount = 1;
        //                    counts.Add(val, currentCount);
        //                }

        //                if (currentCount > maxFreq)
        //                    maxFreq = currentCount;
        //            }
        //        }

        //        // Хвостовой цикл
        //        for (; i < length; i++)
        //        {
        //            int val = ptr[i];

        //            if (counts.TryGetValue(val, out int currentCount))
        //            {
        //                currentCount++;
        //                counts[val] = currentCount;
        //            }
        //            else
        //            {
        //                currentCount = 1;
        //                counts.Add(val, currentCount);
        //            }

        //            if (currentCount > maxFreq)
        //                maxFreq = currentCount;
        //        }
        //    }
        //    return maxFreq;
        //}
    }
}
