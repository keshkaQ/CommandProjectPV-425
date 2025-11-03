using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;

namespace CommandProjectPV_425.Tests
{
    public class CountAboveAverage
    {
        private int[] _array;
        private int _size;
        public CountAboveAverage(int size)
        {
            _size = size;
        }

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            _array = Enumerable.Range(0, _size).Select(x => random.Next(25000)).ToArray();
        }

        //-------------------------------------------------------------------------------------
        //                                                                            Array_For
        //-------------------------------------------------------------------------------------
        [Benchmark(Baseline = true)]
        public int Array_For()
        {
            long sum = 0;
            for (int i = 0; i < _array.Length; i++)
                sum += _array[i];

            double avg = (double)sum / _array.Length;

            int count = 0;
            for (int i = 0; i < _array.Length; i++)
                if (_array[i] > avg) count++;

            return count;
        }

        //-------------------------------------------------------------------------------------
        //                                                                          Array_PLINQ
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public int Array_PLINQ()
        {
            double avg = _array.AsParallel().Average();
            return _array.AsParallel().Count(x => x > avg);
        }

        //-------------------------------------------------------------------------------------
        //                                                                         Parallel_For
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public int Parallel_For()
        {
            // 1. Сумма
            long totalSum = 0;
            Parallel.For(0, _array.Length,
                () => 0L,
                (i, state, localSum) =>
                {
                    localSum += _array[i];
                    return localSum;
                },
                localSum => Interlocked.Add(ref totalSum, localSum));

            double avg = (double)totalSum / _array.Length;

            // 2. Подсчёт элементов
            int totalCount = 0;
            Parallel.For(0, _array.Length,
                () => 0,
                (i, state, localCount) =>
                {
                    if (_array[i] > avg)
                        localCount++;
                    return (int)localCount;
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

        //-------------------------------------------------------------------------------------
        //                                                                      Parallel_Invoke
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public int Parallel_Invoke()
        {
            int cores = Environment.ProcessorCount;
            int chunk = (_array.Length + cores - 1) / cores;
            long totalSum = 0;
            object locker = new object();

            // 🔹 Фаза 1: считаем сумму
            var sumActions = new Action[cores];
            for (int c = 0; c < cores; c++)
            {
                int start = c * chunk;
                int end = Math.Min(start + chunk, _array.Length);

                sumActions[c] = () =>
                {
                    long localSum = 0;
                    for (int i = start; i < end; i++)
                        localSum += _array[i];

                    lock (locker)
                    {
                        totalSum += localSum;
                    }
                };
            }

            Parallel.Invoke(sumActions);

            double avg = (double)totalSum / _array.Length;

            // 🔹 Фаза 2: считаем количество элементов выше среднего
            int totalCount = 0;
            var countActions = new Action[cores];
            for (int c = 0; c < cores; c++)
            {
                int start = c * chunk;
                int end = Math.Min(start + chunk, _array.Length);

                countActions[c] = () =>
                {
                    int localCount = 0;
                    for (int i = start; i < end; i++)
                        if (_array[i] > avg)
                            localCount++;

                    Interlocked.Add(ref totalCount, localCount);
                };
            }

            Parallel.Invoke(countActions);
            return totalCount;
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

            long totalSum = _array.AsParallel().Sum(x => (long)x);
            double avg = (double)totalSum / _array.Length;

            for (int i = 0; i < processorCount; i++)
            {
                int start = i * chunkSize;
                int end = Math.Min(start + chunkSize, _array.Length);

                tasks[i] = Task.Run(() =>
                {
                    int localCount = 0;
                    for (int j = start; j < end; j++)
                        if (_array[j] > avg) localCount++;
                    return localCount;
                });
            }

            Task.WaitAll(tasks);
            return tasks.Sum(t => t.Result);
        }
    }
}
