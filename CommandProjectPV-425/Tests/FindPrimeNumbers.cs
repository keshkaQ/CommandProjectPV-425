using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Collections.Concurrent;

namespace CommandProjectPV_425.Tests
{
    public class FindPrimeNumbers
    {
        private int[]? _array;
        private readonly int _size;

        public FindPrimeNumbers(int size)
        {
            _size = size;
        }

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            _array = Enumerable.Range(0, _size).Select(x => random.Next(25000)).ToArray();
        }

        private static bool IsPrime(int number)
        {
            if (number < 2) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;

            int limit = (int)Math.Sqrt(number);
            for (int i = 3; i <= limit; i += 2)
            {
                if (number % i == 0)
                    return false;
            }
            return true;
        }

        [Benchmark(Baseline = true)]
        public int Array_For()
        {
            int count = 0;
            for (int i = 0; i < _array.Length; i++)
                if (IsPrime(_array[i])) count++;
            return count;
        }

        [Benchmark]
        public int Array_LINQ() => _array.Count(IsPrime);

        [Benchmark]
        public int Array_PLINQ() => _array.AsParallel().Count(IsPrime);


        [Benchmark]
        public int Parallel_ConcurrentBag()
        {
            int total = 0;
            Parallel.ForEach(_array, x =>
            {
                if (IsPrime(x))
                    Interlocked.Increment(ref total);
            });
            return total;
        }

        [Benchmark]
        public int Parallel_For()
        {
            int total = 0;
            Parallel.For(
                0, _array.Length,
                () => 0,
                (i, state, local) =>
                {
                    if (IsPrime(_array[i])) local++;
                    return local;
                },
                local => Interlocked.Add(ref total, local)
            );
            return total;
        }

        [Benchmark]
        public int Parallel_Partitioner()
        {
            int total = 0;
            var partitioner = Partitioner.Create(0, _array.Length);

            Parallel.ForEach(partitioner,
                () => 0,
                (range, state, localCount) =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        if (IsPrime(_array[i]))
                            localCount++;
                    }
                    return localCount;
                },
                localCount => Interlocked.Add(ref total, localCount)
            );
            return total;
        }

        [Benchmark]
        public int Parallel_Invoke()
        {
            int cores = Environment.ProcessorCount;
            int chunk = _array.Length / cores;
            int total = 0;

            var actions = new Action[cores];
            var results = new int[cores];

            for (int c = 0; c < cores; c++)
            {
                int start = c * chunk;
                int end = (c == cores - 1) ? _array.Length : start + chunk;
                int core = c;

                actions[c] = () =>
                {
                    int local = 0;
                    for (int i = start; i < end; i++)
                    {
                        if (IsPrime(_array[i])) local++;
                    }
                    Interlocked.Add(ref total, local);
                };
            }

            Parallel.Invoke(actions);

            for (int c = 0; c < cores; c++)
                total += results[c];

            return total;
        }

        [Benchmark]
        public int Tasks_Run()
        {
            int cores = Environment.ProcessorCount;
            int chunk = (_array.Length + cores - 1) / cores;

            var tasks = new Task<int>[cores];

            for (int c = 0; c < cores; c++)
            {
                int start = c * chunk;
                int end = (c == cores - 1) ? _array.Length : start + chunk;

                tasks[c] = Task.Run(() =>
                {
                    int local = 0;
                    for (int i = start; i < end; i++)
                        if (IsPrime(_array[i]))
                            local++;
                    return local;
                });
            }

            Task.WaitAll(tasks);
            return tasks.Sum(t => t.Result);
        }

        //[Benchmark]
        //public int Parallel_ForEach()
        //{
        //    int total = 0;
        //    Parallel.ForEach(
        //        _array,
        //        () => 0,
        //        (n, state, local) =>
        //        {
        //            if (IsPrime(n)) local++;
        //            return local;
        //        },
        //        local => Interlocked.Add(ref total, local)
        //    );
        //    return total;
        //}

        //[Benchmark]
        //public int Parallel_For_Lists()
        //{
        //    int threadCount = Environment.ProcessorCount;
        //    var results = new int[threadCount];

        //    int chunkSize = (_array.Length + threadCount - 1) / threadCount;

        //    Parallel.For(0, threadCount, threadIdx =>
        //    {
        //        int start = threadIdx * chunkSize;
        //        int end = Math.Min(start + chunkSize, _array.Length);
        //        int localCount = 0;

        //        for (int i = start; i < end; i++)
        //        {
        //            if (IsPrime(_array[i]))
        //                localCount++;
        //        }
        //        results[threadIdx] = localCount;
        //    });

        //    int total = 0;
        //    for (int i = 0; i < threadCount; i++)
        //        total += results[i];

        //    return total;
        //}

        //[Benchmark]
        //public int PLINQ_WithDegreeOfParallelism()
        //{
        //    return _array
        //        .AsParallel()
        //        .WithDegreeOfParallelism(Environment.ProcessorCount)
        //        .Count(IsPrime);
        //}
    }
}