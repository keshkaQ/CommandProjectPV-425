using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;

namespace CommandProjectPV_425.Tests
{
    public class DivisibleThreeOrFive
    {
        private int[] _array;
        private int _size { get; set; }
        public DivisibleThreeOrFive(int size)
        {
            _size = size;
        }

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            _array =  Enumerable.Range(0, _size).Select(x => random.Next(25000)).ToArray();
        }

        private static bool IsDivisibleBy3And5(int x) => x % 3 == 0 && x % 5 == 0;

        //-------------------------------------------------------------------------------------
        //                                                                            Array_For
        //-------------------------------------------------------------------------------------
        [Benchmark(Baseline = true)]
        public int Array_For()
        {
            int count = 0;
            for (int i = 0; i < _array.Length; i++)
                if (IsDivisibleBy3And5(_array[i]))
                    count++;
            return count;
        }

        //-------------------------------------------------------------------------------------
        //                                                                          Array_PLINQ
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public int Array_PLINQ()
        {
            return _array.AsParallel().Count(IsDivisibleBy3And5);
        }

        //-------------------------------------------------------------------------------------
        //                                                                         Parallel_For
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public int Parallel_For()
        {
            int totalCount = 0;
            Parallel.For(0, _array.Length,
               () => 0,
               (i, loopstate, localCount) =>
               {
                   if (IsDivisibleBy3And5(_array[i]))
                       localCount++;
                   return localCount;
               },
               localCount => Interlocked.Add(ref totalCount, localCount));

            return totalCount;
        }

        //-------------------------------------------------------------------------------------
        //                                                                 Parallel_Partitioner
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public int Parallel_Partitioner()
        {
            int totalCount = 0;
            Parallel.ForEach(Partitioner.Create(0, _array.Length),
                () => 0,
                (range, state, localCount) =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                        if (IsDivisibleBy3And5(_array[i]))
                            localCount++;
                    return localCount;
                },
                localCount => Interlocked.Add(ref totalCount, localCount));

            return totalCount;
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
                        if (IsDivisibleBy3And5(_array[i])) local++;
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
            int processorCount = Environment.ProcessorCount;
            int chunkSize = (_array.Length + processorCount - 1) / processorCount;

            var tasks = new Task<int>[processorCount];

            for (int i = 0; i < processorCount; i++)
            {
                int start = i * chunkSize;
                int end = (i == processorCount - 1) ? _array.Length : start + chunkSize;

                tasks[i] = Task.Run(() =>
                {
                    int localCount = 0;
                    for (int j = start; j < end; j++)
                        if (IsDivisibleBy3And5(_array[i]))
                            localCount++;
                    return localCount;
                });
            }

            Task.WaitAll(tasks);
            return tasks.Sum(t => t.Result);
        }
    }
}