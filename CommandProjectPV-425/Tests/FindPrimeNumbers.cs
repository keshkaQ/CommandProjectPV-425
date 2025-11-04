using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.Intrinsics.X86;

namespace CommandProjectPV_425.Tests
{
    public class FindPrimeNumbers
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
        public int Array_PLINQ() => _array.AsParallel().Count(IsPrime);

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


        [Benchmark]
        public unsafe int Array_Unsafe()
        {
            int length = _array.Length;
            int count0 = 0, count1 = 0, count2 = 0, count3 = 0;

            fixed (int* p = _array)
            {
                int* ptr = p;
                int* endVectorized = p + (length & ~3);

                while (ptr < endVectorized)
                {
                    int n0 = ptr[0], n1 = ptr[1], n2 = ptr[2], n3 = ptr[3];
                    if (IsPrime(n0)) count0++;
                    if (IsPrime(n1)) count1++;
                    if (IsPrime(n2)) count2++;
                    if (IsPrime(n3)) count3++;
                    ptr += 4;
                }

                for (; ptr < p + length; ptr++)
                {
                    if (IsPrime(*ptr)) count0++;
                }
            }

            return count0 + count1 + count2 + count3;
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
                // Избегаем bounds check в v[j]
                for (int j = 0; j < vectorSize; j++)
                {
                    int n = v[j];
                    if (IsPrime(n)) count++;
                }
            }

            // Обработка остатка
            for (; i < length; i++)
            {
                int n = array[i];
                if (IsPrime(n)) count++;
            }

            return count;
        }

        [Benchmark]
        public unsafe int Array_SIMD_Intrinsics()
        {
            if (!Avx2.IsSupported)
                throw new PlatformNotSupportedException("AVX2 not supported on this CPU");

            var array = _array;
            int length = array.Length;
            int count = 0;

            fixed (int* ptr = array)
            {
                int i = 0;
                int limit = length - (length % 8);

                int* temp = stackalloc int[8]; // Выделяем один раз

                for (; i < limit; i += 8)
                {
                    var v = Avx2.LoadVector256(ptr + i);
                    Avx2.Store(temp, v);

                    for (int j = 0; j < 8; j++)
                    {
                        int n = temp[j];
                        if (IsPrime(n)) count++;
                    }
                }

                for (; i < length; i++)
                {
                    int n = ptr[i];
                    if (IsPrime(n)) count++;
                }
            }

            return count;
        }
    }
}