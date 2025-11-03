using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;

namespace CommandProjectPV_425.Tests
{
    public class MaximumOfNonExtremeElements
    {
        private readonly int _size;
        public int[]? _array;
        public MaximumOfNonExtremeElements(int size)
        {
            _size = size;
        }

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            _array = Enumerable.Range(0, _size).Select(x => random.Next(25000)).ToArray();
        }

        public bool IsLocalMinOrMax(IList<int> numbers, int index)
        {
            if (numbers.Count <= 1) return false;

            if (index == 0)
                return numbers[0] > numbers[1] || numbers[0] < numbers[1];

            if (index == numbers.Count - 1)
                return numbers[^1] > numbers[^2] || numbers[^1] < numbers[^2];

            bool isMin = numbers[index] < numbers[index - 1] && numbers[index] < numbers[index + 1];
            bool isMax = numbers[index] > numbers[index - 1] && numbers[index] > numbers[index + 1];

            return isMin || isMax;
        }

        [Benchmark(Baseline = true)]
        public int Array_For()
        {
            int max = int.MinValue;
            for (int i = 0; i < _array.Length; i++)
                if (!IsLocalMinOrMax(_array, i) && _array[i] > max)
                    max = _array[i];
            return max;
        }

        [Benchmark]
        public int Array_LINQ()
        {
            var nonExtreme = _array.Where((x, i) => !IsLocalMinOrMax(_array, i));
            return nonExtreme.Any() ? nonExtreme.Max() : int.MinValue;
        }

        [Benchmark]
        public int Array_PLINQ()
        {
            var nonExtreme = _array.AsParallel().Where((x, i) => !IsLocalMinOrMax(_array, i));
            return nonExtreme.Any() ? nonExtreme.Max() : int.MinValue;
        }

        [Benchmark]
        public int Parallel_ConcurrentBag()
        {
            var bag = new ConcurrentBag<int>();

            Parallel.For(0, _array.Length, i =>
            {
                if (!IsLocalMinOrMax(_array, i))
                {
                    bag.Add(_array[i]);
                }
            });

            return bag.Count > 0 ? bag.Max() : int.MinValue;
        }

        [Benchmark]
        public int Parallel_For()
        {
            int finalMax = int.MinValue;
            var locker = new object();

            Parallel.For(0, _array.Length,
                () => int.MinValue,
                (i, state, localMax) =>
                {
                    if (!IsLocalMinOrMax(_array, i))
                    {
                        if (_array[i] > localMax)
                        {
                            localMax = _array[i];
                        }
                    }
                    return localMax;
                },
                localMax =>
                {
                    lock (locker)
                    {
                        if (localMax > finalMax)
                        {
                            finalMax = localMax;
                        }
                    }
                });

            return finalMax;
        }

        [Benchmark]
        public int Parallel_Partitioner()
        {
            int finalMax = int.MinValue;
            object locker = new();

            Parallel.ForEach(Partitioner.Create(0, _array.Length),
                () => int.MinValue,
                (range, state, localMax) =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        if (!IsLocalMinOrMax(_array, i))
                        {
                            if (_array[i] > localMax)
                            {
                                localMax = _array[i];
                            }
                        }
                    }
                    return localMax;
                },
                localMax =>
                {
                    lock (locker)
                    {
                        if (localMax > finalMax)
                        {
                            finalMax = localMax;
                        }
                    }
                });

            return finalMax;
        }

        [Benchmark]
        public int Parallel_Invoke()
        {
            int cores = Environment.ProcessorCount;
            int chunk = (_array.Length + cores - 1) / cores;
            int globalMax = int.MinValue;
            object locker = new object();

            var actions = new Action[cores];

            for (int c = 0; c < cores; c++)
            {
                int start = c * chunk;
                int end = Math.Min(start + chunk, _array.Length);

                actions[c] = () =>
                {
                    int localMax = int.MinValue;

                    // чтобы не обращаться за пределы массива
                    int safeStart = Math.Max(1, start);
                    int safeEnd = Math.Min(end, _array.Length - 1);

                    for (int i = safeStart; i < safeEnd; i++)
                    {
                        if (!IsLocalMinOrMax(_array, i))
                        {
                            if (_array[i] > localMax)
                                localMax = _array[i];
                        }
                    }

                    lock (locker)
                    {
                        if (localMax > globalMax)
                            globalMax = localMax;
                    }
                };
            }

            Parallel.Invoke(actions);
            return globalMax;
        }

        [Benchmark]
        public int Tasks_Run()
        {
            int processorCount = Environment.ProcessorCount;
            int chunkSize = (_array.Length + processorCount - 1) / processorCount;

            var tasks = new Task<int>[processorCount];

            for (int i = 0; i < processorCount; i++)
            {
                int start = i * chunkSize;
                int end = Math.Min(start + chunkSize, _array.Length);

                tasks[i] = Task.Run(() =>
                {
                    int localMax = int.MinValue;
                    for (int j = start; j < end; j++)
                        if (!IsLocalMinOrMax(_array, j) && _array[j] > localMax)
                            localMax = _array[j];
                    return localMax;
                });
            }

            Task.WaitAll(tasks);

            return tasks.Max(t => t.Result);
        }

        //[Benchmark]
        //public int List_For()
        //{
        //    int maxValue = int.MinValue;
        //    bool foundAny = false;

        //    for (int i = 0; i < _list.Count; i++)
        //    {
        //        if (!IsLocalMinOrMax(_list, i))
        //        {
        //            foundAny = true;
        //            if (_list[i] > maxValue)
        //            {
        //                maxValue = _list[i];
        //            }
        //        }
        //    }

        //    return foundAny ? maxValue : int.MinValue;
        //}


        //[Benchmark]
        //public int List_LINQ()
        //{
        //    var nonExtreme = _list.Where((x, i) => !IsLocalMinOrMax(_list, i));
        //    return nonExtreme.Any() ? nonExtreme.Max() : int.MinValue;
        //}

        //[Benchmark]
        //public int List_PLINQ()
        //{
        //    var nonExtreme = _list.AsParallel().Where((x, i) => !IsLocalMinOrMax(_list, i));
        //    return nonExtreme.Any() ? nonExtreme.Max() : int.MinValue;
        //}
    }
}