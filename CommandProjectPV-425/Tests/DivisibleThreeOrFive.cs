using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;

namespace CommandProjectPV_425.Tests
{
    public class DivisibleThreeOrFive
    {
        private int[] _array;
        private List<int> _list;
        private int _size { get; set; }
        public DivisibleThreeOrFive(int size)
        {
            _size = size;
        }

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            _list = [.. Enumerable.Range(0, _size).Select(x => random.Next(25000))];
            _array = _list.ToArray();
        }

        [Benchmark(Baseline = true)]
        public int Array_For()
        {
            int count = 0;
            for (int i = 0; i < _array.Length; i++)
            {
                if (_array[i] % 3 == 0 && _array[i] % 5 == 0)
                {
                    count++;
                }
            }
            return count;
        }

        [Benchmark]
        public int List_For()
        {
            int count = 0;
            for (int i = 0; i < _list.Count; i++)
            {
                if (_list[i] % 3 == 0 && _list[i] % 5 == 0)
                {
                    count++;
                }
            }
            return count;
        }

        [Benchmark]
        public int Array_LINQ()
        {
            return _array.Count(x => x % 3 == 0 && x % 5 == 0);
        }

        [Benchmark]
        public int List_LINQ()
        {
            return _list.Count(x => x % 3 == 0 && x % 5 == 0);
        }

        [Benchmark]
        public int Parallel_ForEach_Partitioner()
        {
            int totalCount = 0;
            Parallel.ForEach(Partitioner.Create(0, _array.Length),
                () => 0,
                (range, state, localCount) =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                        if (_array[i] % 3 == 0 && _array[i] % 5 == 0)
                            localCount++;
                    return localCount;
                },
                localCount => Interlocked.Add(ref totalCount, localCount));

            return totalCount;
        }

        [Benchmark]
        public int Parallel_For_Local()
        {
            int totalCount = 0;
            Parallel.For(0, _array.Length,
               () => 0,
               (i, loopstate, localCount) =>
               {
                   if (_array[i] % 3 == 0 && _array[i] % 5 == 0)
                       localCount++;
                   return localCount;
               },
               localCount => Interlocked.Add(ref totalCount, localCount));

            return totalCount;
        }

        [Benchmark]
        public int Parallel_ConcurrentBag()
        {
            var bag = new ConcurrentBag<int>();
            Parallel.ForEach(_array, item =>
            {
                if (item % 3 == 0 && item % 5 == 0)
                    bag.Add(item);
            });

            return bag.Count;
        }

        [Benchmark]
        public int PLINQ_AutoParallel()
        {
            return _array.AsParallel().Count(x => x % 3 == 0 && x % 5 == 0);
        }

        [Benchmark]
        public int PLINQ_WithDegreeOfParallelism()
        {
            return _array.AsParallel()
                       .WithDegreeOfParallelism(Environment.ProcessorCount)
                       .Count(x => x % 3 == 0 && x % 5 == 0);
        }

        [Benchmark]
        public int PLINQ_ForceParallel()
        {
            return _array.AsParallel()
                       .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                       .Count(x => x % 3 == 0 && x % 5 == 0);
        }

        [Benchmark]
        public int Tasks_Run()
        {
            int processorCount = Environment.ProcessorCount;
            int chunkSize = _array.Length / processorCount;

            var tasks = new Task<int>[processorCount];
            for (int i = 0; i < processorCount; i++)
            {
                int start = i * chunkSize;
                int end = (i == processorCount - 1) ? _array.Length : start + chunkSize;

                tasks[i] = Task.Run(() =>
                {
                    int localCount = 0;
                    for (int j = start; j < end; j++)
                        if (_array[j] % 3 == 0 && _array[j] % 5 == 0)
                            localCount++;
                    return localCount;
                });
            }

            return Task.WhenAll(tasks).Result.Sum();
        }

        [Benchmark]
        public int Tasks_Factory()
        {
            int processorCount = Environment.ProcessorCount;
            int chunkSize = _array.Length / processorCount;

            var tasks = new Task<int>[processorCount];
            for (int i = 0; i < processorCount; i++)
            {
                int start = i * chunkSize;
                int end = (i == processorCount - 1) ? _array.Length : start + chunkSize;

                tasks[i] = Task.Factory.StartNew(() =>
                {
                    int localCount = 0;
                    for (int j = start; j < end; j++)
                        if (_array[j] % 3 == 0 && _array[j] % 5 == 0)
                            localCount++;
                    return localCount;
                });
            }

            return Task.WhenAll(tasks).Result.Sum();
        }
    }
}