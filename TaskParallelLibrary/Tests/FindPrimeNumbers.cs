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

        //-------------------------------------------------------------------------------------
        //                                                                            Array_For
        //-------------------------------------------------------------------------------------
        [Benchmark(Baseline = true)]
        public int Array_For()
        {
            int count = 0;
            for (int i = 0; i < _array.Length; i++)
                if (IsPrime(_array[i])) count++;
            return count;
        }

        //-------------------------------------------------------------------------------------
        //                                                                           Array_LINQ
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public int Array_LINQ()
        {
            return _array.Count(IsPrime);
        }

        //-------------------------------------------------------------------------------------
        //                                                                          Array_PLINQ
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public int Array_PLINQ()
        {
            return _array.AsParallel().Count(IsPrime);
        }

        //-------------------------------------------------------------------------------------
        //                                                               Parallel_ConcurrentBag
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public int Parallel_ConcurrentBag()
        {
            var bag = new ConcurrentBag<int>();
            Parallel.ForEach(_array, n =>
            {
                if (IsPrime(n))
                    bag.Add(n);
            });
            return bag.Count;
        }

        //-------------------------------------------------------------------------------------
        //                                                                         Parallel_For
        //-------------------------------------------------------------------------------------
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

        //-------------------------------------------------------------------------------------
        //                                                                 Parallel_Partitioner
        //-------------------------------------------------------------------------------------
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

        //-------------------------------------------------------------------------------------
        //                                                                      Parallel_Invoke
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public int Parallel_Invoke()
        {
            int cores = Environment.ProcessorCount;
            int chunk = (_array.Length + cores - 1) / cores;
            int total = 0;

            var actions = new Action[cores];

            for (int c = 0; c < cores; c++)
            {
                int start = c * chunk;
                int end = Math.Min(start + chunk, _array.Length);

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

            return total;
        }

        //-------------------------------------------------------------------------------------
        //                                                                            Tasks_Run
        //-------------------------------------------------------------------------------------
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
    }
}