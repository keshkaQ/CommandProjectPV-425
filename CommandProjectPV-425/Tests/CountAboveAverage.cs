using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;

namespace CommandProjectPV_425.Tests
{
    public class CountAboveAverage
    {
        private int[] _array;
        private List<int> _list;
        private int _size;
        public CountAboveAverage(int size)
        {
            _size = size;
        }

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            _list = new List<int>(Enumerable.Range(0, _size).Select(x => random.Next(25000)));
            _array = _list.ToArray();
        }
        [Benchmark]
        public int Parallel_ConcurrentBag()
        {
            double avg = _array.AsParallel().Average();
            var bag = new ConcurrentBag<int>();
            Parallel.ForEach(_array, item =>
            {
                if (item > avg)
                    bag.Add(item);
            });

            return bag.Count;
        }

        [Benchmark]
        public int List_Foreach()
        {
            long total = 0;
            int count = 0;
            foreach (var item in _list)
                total += item;
            double avg = (double)total / _list.Count;
            foreach (var item in _list)
                if (item > avg)
                    count++;
            return count;
        }

        [Benchmark]
        public int Array_LINQ()
        {
            double avg = _array.Average();
            return _array.Count(x => x > avg);
        }

        [Benchmark]
        public int List_LINQ()
        {
            double avg = _list.Average();
            return _list.Count(x => x > avg);
        }

        [Benchmark]
        public int Parallel_For_Local()
        {
            long totalSum = 0;
            Parallel.For(0, _array.Length,
                () => 0L,
                (i, loopstate, localSum) =>
                {
                    localSum += _array[i];
                    return localSum;
                },
                localSum => Interlocked.Add(ref totalSum, localSum));

            double avg = (double)(totalSum / _array.Length);

            int totalCount = 0;
            Parallel.For(0, _array.Length,
               () => 0,
               (i, loopstate, localCount) =>
               {
                   if (_array[i] > avg)
                       localCount++;
                   return (int)localCount;
               },
               localCount => Interlocked.Add(ref totalCount, localCount));

            return totalCount;
        }

        [Benchmark]
        public int PLINQ_AutoParallel()
        {
            double avg = _array.AsParallel().Average();
            return _array.AsParallel().Count(x => x > avg);
        }

        [Benchmark]
        public int PLINQ_WithDegreeOfParallelism()
        {
            double avg = _array.AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount).Average();
            return _array.AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount).Count(x => x > avg);
        }

        [Benchmark]
        public int PLINQ_ForceParallel()
        {
            double avg = _array.AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).Average();
            return _array.AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).Count(x => x > avg);
        }

        [Benchmark]
        public int Array_For()
        {
            long sum = 0;
            for (int i = 0; i < _array.Length; i++)
            {
                sum += _array[i];
            }

            double avg = (double)sum / _array.Length;

            int countAboveAverage = 0;
            for (int i = 0; i < _array.Length; i++)
            {
                if (_array[i] > avg)
                {
                    countAboveAverage++;
                }
            }
            return countAboveAverage;
        }
        [Benchmark]
        public int List_For()
        {
            long sum = 0;
            for (int i = 0; i < _list.Count; i++)
            {
                sum += _list[i];
            }

            double avg = (double)sum / _list.Count;

            int countAboveAverage = 0;
            for (int i = 0; i < _list.Count; i++)
            {
                if (_list[i] > avg)
                {
                    countAboveAverage++;
                }
            }
            return countAboveAverage;
        }
        [Benchmark]
        public int Parallel_ForEach_Partitioner()
        {
            long totalSum = 0;
            Parallel.ForEach(Partitioner.Create(0, _array.Length),
                () => 0L,
                (range, state, localSum) =>
                {
                    // range - Tuple<int, int> (начало, конец диапазона)
                    // state - информация о состоянии цикла (можно прервать)
                    // localSum - локальная сумма потока

                    for (int i = range.Item1; i < range.Item2; i++)
                        localSum += _array[i];
                    return localSum;
                },
                // localSum - финальная сумма из одного потока
                localSum => Interlocked.Add(ref totalSum, localSum)); // Потокобезопасное сложение

            double avg = (double)totalSum / _array.Length;

            int totalCount = 0;
            Parallel.ForEach(Partitioner.Create(0, _array.Length),
                () => 0,
                (range, state, localCount) =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                        if (_array[i] > avg) localCount++;
                    return localCount;
                },
                localCount => Interlocked.Add(ref totalCount, localCount));

            return totalCount;
        }

        [Benchmark]
        public int Tasks_Run()
        {
            int processorCount = Environment.ProcessorCount;
            int chunkSize = _array.Length / processorCount;

            var sumTasks = new Task<long>[processorCount];
            for (int i = 0; i < processorCount; i++)
            {
                int start = i * chunkSize;
                int end = (i == processorCount - 1) ? _array.Length : start + chunkSize;

                sumTasks[i] = Task.Run(() =>
                {
                    long localSum = 0;
                    for (int j = start; j < end; j++)
                        localSum += _array[j];
                    return localSum;
                });
            }

            long totalSum = Task.WhenAll(sumTasks).Result.Sum();
            double avg = (double)totalSum / _array.Length;

            var countTasks = new Task<int>[processorCount];
            for (int i = 0; i < processorCount; i++)
            {
                int start = i * chunkSize;
                int end = (i == processorCount - 1) ? _array.Length : start + chunkSize;

                countTasks[i] = Task.Run(() =>
                {
                    int localCount = 0;
                    for (int j = start; j < end; j++)
                        if (_array[j] > avg) localCount++;
                    return localCount;
                });
            }

            return Task.WhenAll(countTasks).Result.Sum();
        }
        [Benchmark]
        public int Tasks_Factory()
        {
            int processorCount = Environment.ProcessorCount;
            int chunkSize = _array.Length / processorCount;

            var sumTasks = new Task<long>[processorCount];
            for (int i = 0; i < processorCount; i++)
            {
                int start = i * chunkSize;
                int end = (i == processorCount - 1) ? _array.Length : start + chunkSize;

                sumTasks[i] = Task.Factory.StartNew(() =>
                {
                    long localSum = 0;
                    for (int j = start; j < end; j++)
                        localSum += _array[j];
                    return localSum;
                });
            }

            long totalSum = Task.WhenAll(sumTasks).Result.Sum();
            double avg = (double)totalSum / _array.Length;

            var countTasks = new Task<int>[processorCount];
            for (int i = 0; i < processorCount; i++)
            {
                int start = i * chunkSize;
                int end = (i == processorCount - 1) ? _array.Length : start + chunkSize;

                countTasks[i] = Task.Factory.StartNew(() =>
                {
                    int localCount = 0;
                    for (int j = start; j < end; j++)
                        if (_array[j] > avg) localCount++;
                    return localCount;
                });
            }

            return Task.WhenAll(countTasks).Result.Sum();
        }
    }
}
