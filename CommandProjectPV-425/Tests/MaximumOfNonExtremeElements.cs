using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;

namespace CommandProjectPV_425.Tests
{
    public class MaximumOfNonExtremeElements
    {
        private readonly int _size;
        public List<int>? _list;
        public int[]? _array;
        public MaximumOfNonExtremeElements(int size)
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
            int maxValue = int.MinValue;
            bool foundAny = false;

            for (int i = 0; i < _array.Length; i++)
            {
                if (!IsLocalMinOrMax(_array, i))
                {
                    foundAny = true;
                    if (_array[i] > maxValue)
                    {
                        maxValue = _array[i];
                    }
                }
            }

            return foundAny ? maxValue : int.MinValue;
        }

        [Benchmark]
        public int List_For()
        {
            int maxValue = int.MinValue;
            bool foundAny = false;

            for (int i = 0; i < _list.Count; i++)
            {
                if (!IsLocalMinOrMax(_list, i))
                {
                    foundAny = true;
                    if (_list[i] > maxValue)
                    {
                        maxValue = _list[i];
                    }
                }
            }

            return foundAny ? maxValue : int.MinValue;
        }

        [Benchmark]
        public int Parallel_For_ConcurrentBag()
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
        public int Parallel_Foreach_Partitioner()
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
        public int List_LINQ()
        {
            var nonExtreme = _list.Where((x, i) => !IsLocalMinOrMax(_list, i));
            return nonExtreme.Any() ? nonExtreme.Max() : int.MinValue;
        }

        [Benchmark]
        public int Array_LINQ()
        {
            var nonExtreme = _array.Where((x, i) => !IsLocalMinOrMax(_array, i));
            return nonExtreme.Any() ? nonExtreme.Max() : int.MinValue;
        }

        [Benchmark]
        public int List_PLINQ()
        {
            var nonExtreme = _list.AsParallel().Where((x, i) => !IsLocalMinOrMax(_list, i));
            return nonExtreme.Any() ? nonExtreme.Max() : int.MinValue;
        }

        [Benchmark]
        public int Array_PLINQ()
        {
            var nonExtreme = _array.AsParallel().Where((x, i) => !IsLocalMinOrMax(_array, i));
            return nonExtreme.Any() ? nonExtreme.Max() : int.MinValue;
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
                    bool foundAny = false;

                    for (int j = start; j < end; j++)
                    {
                        if (!IsLocalMinOrMax(_array, j))
                        {
                            foundAny = true;
                            if (_array[j] > localMax)
                            {
                                localMax = _array[j];
                            }
                        }
                    }

                    return foundAny ? localMax : int.MinValue;
                });
            }

            Task.WaitAll(tasks);

            int globalMax = int.MinValue;
            foreach (var task in tasks)
            {
                int taskResult = task.Result;
                if (taskResult > globalMax)
                {
                    globalMax = taskResult;
                }
            }

            return globalMax;
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
    }
}