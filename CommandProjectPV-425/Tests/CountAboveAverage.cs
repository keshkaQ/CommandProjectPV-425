using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace CommandProjectPV_425.Tests
{
    public class CountAboveAverage
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

        [Benchmark(Baseline = true)]
        public int Array_For()
        {
            long sum = 0;
            for (int i = 0; i < _array.Length; i++)
                sum += _array[i];

            double avg = (double)sum / _array.Length;

            int countAboveAverage = 0;
            for (int i = 0; i < _array.Length; i++)
            {
                if (_array[i] > avg) countAboveAverage++;
            }
            return countAboveAverage;
        }

        [Benchmark]
        public int Array_PLINQ()
        {
            double avg = _array.AsParallel().Average();
            return _array.AsParallel().Count(x => x > avg);
        }

        [Benchmark]
        public int Parallel_For()
        {
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

            int totalCount = 0;
            Parallel.For(0, _array.Length,
               () => 0,
               (i, state, localCount) =>
               {
                   if (_array[i] > avg)
                       localCount++;
                   return localCount;
               },
               localCount => Interlocked.Add(ref totalCount, localCount));

            return totalCount;
        }

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

        [Benchmark]
        public int Parallel_Invoke()
        {
            int cores = Environment.ProcessorCount;
            int chunk = (_array.Length + cores - 1) / cores;
            long totalSum = 0;
            object locker = new object();

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

        //-------------------------------------------------------------------------------------
        //                                                                         Array_Unsafe
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public unsafe int Array_Unsafe()
        {
            int length = _array.Length;
            long sum0 = 0, sum1 = 0, sum2 = 0, sum3 = 0; // 4 аккумулятора для разворачивания

            fixed (int* p = _array)
            {
                int* ptr = p;
                int* end = p + length;
                int* endVectorized = p + (length & ~3); // Обрабатываем блоками по 4 (length - length % 4)

                // ---- 1. Сумма: Развернутый цикл (шаг 4)
                while (ptr < endVectorized)
                {
                    sum0 += ptr[0];
                    sum1 += ptr[1];
                    sum2 += ptr[2];
                    sum3 += ptr[3];
                    ptr += 4;
                }

                // Остаток
                while (ptr < end)
                {
                    sum0 += *ptr;
                    ptr++;
                }

                long totalSum = sum0 + sum1 + sum2 + sum3;
                double avg = (double)totalSum / length;

                // ---- 2. Подсчёт: Развернутый цикл (шаг 4)
                ptr = p;
                int count0 = 0, count1 = 0, count2 = 0, count3 = 0;

                while (ptr < endVectorized)
                {
                    if (ptr[0] > avg) count0++;
                    if (ptr[1] > avg) count1++;
                    if (ptr[2] > avg) count2++;
                    if (ptr[3] > avg) count3++;
                    ptr += 4;
                }

                // Остаток
                while (ptr < end)
                {
                    if (*ptr > avg)
                        count0++;
                    ptr++;
                }

                return count0 + count1 + count2 + count3;
            }
        }

        //-------------------------------------------------------------------------------------
        //                                                                           Array_SIMD
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public int Array_SIMD()
        {
            var array = _array;
            int length = array.Length;
            int vectorSize = Vector<int>.Count;

            // ---- 1. Векторная сумма (с расширением до long)
            Vector<long> vSum0 = Vector<long>.Zero;
            Vector<long> vSum1 = Vector<long>.Zero;
            int i = 0;

            for (; i <= length - vectorSize; i += vectorSize)
            {
                var vInt = new Vector<int>(array, i);
                Vector.Widen(vInt, out Vector<long> vLow, out Vector<long> vHigh);
                vSum0 += vLow;
                vSum1 += vHigh;
            }

            long sum = Vector.Sum(vSum0) + Vector.Sum(vSum1);
            for (; i < length; i++)
                sum += array[i];

            double avg = (double)sum / length;

            // ---- 2. Подсчёт элементов > avg
            int count = 0;
            i = 0;
            var vAvgInt = new Vector<int>((int)avg);
            Vector<int> vCount = Vector<int>.Zero;

            for (; i <= length - vectorSize; i += vectorSize)
            {
                var v = new Vector<int>(array, i);
                var mask = Vector.GreaterThan(v, vAvgInt); // -1 там, где > avg
                vCount += mask;
            }

            // Маска = -1 → сумма отрицательная, инвертируем
            count = -Vector.Sum(vCount);

            // Остаток
            for (; i < length; i++)
                if (array[i] > avg)
                    count++;

            return count;
        }

        //-------------------------------------------------------------------------------------
        //                                                                Array_SIMD_Intrinsics
        //-------------------------------------------------------------------------------------
        [Benchmark]
        public unsafe int Array_SIMD_Intrinsics()
        {
            if (!Avx2.IsSupported)
                throw new PlatformNotSupportedException("AVX2 not supported on this CPU");

            int length = _array.Length;
            long total = 0;

            fixed (int* ptr = _array)
            {
                int i = 0;
                int limit = length & ~7; // кратно 8 элементам (2 группы по 4 int)

                Vector256<long> vSum0 = Vector256<long>.Zero;
                Vector256<long> vSum1 = Vector256<long>.Zero;

                // ---- 1. Сумма (векторизованное сложение int -> long)
                for (; i < limit; i += 8)
                {
                    // 1-я группа из 4 int
                    var v0 = Avx2.ConvertToVector256Int64(ptr + i);
                    // 2-я группа из 4 int
                    var v1 = Avx2.ConvertToVector256Int64(ptr + i + 4);

                    vSum0 = Avx2.Add(vSum0, v0);
                    vSum1 = Avx2.Add(vSum1, v1);
                }

                // горизонтальное суммирование
                long* tmp = stackalloc long[4];
                Avx.Store(tmp, vSum0);
                for (int j = 0; j < 4; j++) total += tmp[j];
                Avx.Store(tmp, vSum1);
                for (int j = 0; j < 4; j++) total += tmp[j];

                // остаток
                for (; i < length; i++)
                    total += ptr[i];

                double avg = (double)total / length;

                // ---- 2. Подсчёт элементов > avg
                int count = 0;
                i = 0;
                limit = length & ~3; // по 4 int за итерацию

                Vector256<double> vAvg = Vector256.Create(avg);

                for (; i < limit; i += 4)
                {
                    // загружаем 4 int → 4 double
                    Vector256<double> v = Avx.ConvertToVector256Double(Avx.LoadVector128(ptr + i));

                    // сравнение: v > avg
                    var mask = Avx.Compare(v, vAvg, FloatComparisonMode.OrderedGreaterThanNonSignaling);

                    // получаем 4-битную маску (1 бит на элемент)
                    uint bits = (uint)Avx.MoveMask(mask);
                    count += BitOperations.PopCount(bits);
                }

                // остаток (скалярно)
                for (; i < length; i++)
                    if (ptr[i] > avg)
                        count++;

                return count;
            }
        }
    }
}
