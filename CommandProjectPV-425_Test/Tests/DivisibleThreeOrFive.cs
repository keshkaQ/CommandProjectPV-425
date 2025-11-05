using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace CommandProjectPV_425_Test.Tests
{
    public class DivisibleThreeOrFive
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

        private static bool IsDivisibleBy3And5(int x) => x % 3 == 0 && x % 5 == 0;

        [Benchmark(Baseline = true)]
        public int Array_For()
        {
            int count = 0;
            for (int i = 0; i < _array.Length; i++)
                if (IsDivisibleBy3And5(_array[i]))
                    count++;
            return count;
        }

        [Benchmark]
        public int Array_PLINQ() =>_array.AsParallel().Count(IsDivisibleBy3And5);

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


        [Benchmark]
        public unsafe int Array_Unsafe()
        {
            int length = _array.Length;
            int count = 0;

            fixed (int* ptr = _array)
            {
                int* p = ptr;
                int* end = ptr + length;

                while (p + 8 <= end)
                {
                    if (IsDivisibleBy3And5(p[0])) count++;
                    if (IsDivisibleBy3And5(p[1])) count++;
                    if (IsDivisibleBy3And5(p[2])) count++;
                    if (IsDivisibleBy3And5(p[3])) count++;
                    if (IsDivisibleBy3And5(p[4])) count++;
                    if (IsDivisibleBy3And5(p[5])) count++;
                    if (IsDivisibleBy3And5(p[6])) count++;
                    if (IsDivisibleBy3And5(p[7])) count++;
                    p += 8;
                }

                // Остаток
                while (p < end)
                {
                    if (IsDivisibleBy3And5(*p))
                        count++;
                    p++;
                }
            }

            return count;
        }

        [Benchmark]
        public int Array_SIMD()
        {
            var array = _array;
            int length = array.Length;
            int vectorSize = Vector<int>.Count;
            int count = 0;
            int i = 0;

            for (; i <= length - vectorSize; i += vectorSize)
            {
                var v = new Vector<int>(array, i);

                // Проверяем без вызова функции
                for (int j = 0; j < vectorSize; j++)
                    count += (IsDivisibleBy3And5(v[j])) ? 1 : 0;
            }

            for (; i < length; i++)
                count += (IsDivisibleBy3And5(array[i])) ? 1 : 0;

            return count;
        }


        [Benchmark]
        public unsafe int Array_SIMD_Intrinsics()
        {
            if (!Avx2.IsSupported)
                throw new PlatformNotSupportedException("AVX2 not supported on this CPU");

            int length = _array.Length;
            int count = 0;

            fixed (int* ptr = _array)
            {
                int i = 0;

                var vCount = Vector256<int>.Zero;
                var vOnes = Vector256.Create(1);
                int vectorSize = 8;

                for (; i <= length - vectorSize; i += vectorSize)
                {
                    var v = Avx2.LoadVector256(ptr + i);

                    // Проверяем каждый элемент скалярно (самый надежный способ)
                    for (int j = 0; j < vectorSize; j++)
                    {
                        if (IsDivisibleBy3And5(v.GetElement(j)))
                            count++;
                    }
                }

                // Обработка остатка
                for (; i < length; i++)
                {
                    if (IsDivisibleBy3And5(ptr[i]))
                        count++;
                }
            }

            return count;
        }
    }
}