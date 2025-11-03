using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace TaskParallelLibrary.Tests;

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

    private static bool IsLocalMinOrMax(IList<int> numbers, int index)
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

    //-------------------------------------------------------------------------------------
    //                                                                            Array_For
    //-------------------------------------------------------------------------------------
    [Benchmark(Baseline = true)]
    public int Array_For()
    {
        int max = int.MinValue;
        for (int i = 0; i < _array.Length; i++)
            if (!IsLocalMinOrMax(_array, i) && _array[i] > max)
                max = _array[i];
        return max;
    }

    //-------------------------------------------------------------------------------------
    //                                                                          Array_PLINQ
    //-------------------------------------------------------------------------------------
    [Benchmark]
    public int Array_PLINQ()
    {
        var nonExtreme = _array.AsParallel().Where((x, i) => !IsLocalMinOrMax(_array, i));
        return nonExtreme.Any() ? nonExtreme.Max() : int.MinValue;
    }

    //-------------------------------------------------------------------------------------
    //                                                                         Parallel_For
    //-------------------------------------------------------------------------------------
    [Benchmark]
    public int Parallel_For()
    {
        int finalMax = int.MinValue;
        var locker = new object();

        Parallel.For(0, _array.Length,
            () => int.MinValue,
            (i, state, localMax) =>
            {
                if (!IsLocalMinOrMax(_array, i) && _array[i] > localMax)
                    localMax = _array[i];
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

    //-------------------------------------------------------------------------------------
    //                                                                 Parallel_Partitioner
    //-------------------------------------------------------------------------------------
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
                    if (!IsLocalMinOrMax(_array, i) && _array[i] > localMax)
                        localMax = _array[i];
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

    //-------------------------------------------------------------------------------------
    //                                                                      Parallel_Invoke
    //-------------------------------------------------------------------------------------
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

    //-------------------------------------------------------------------------------------
    //                                                                         Array_Unsafe
    //-------------------------------------------------------------------------------------
    [Benchmark]
    public unsafe int Array_Unsafe()
    {
        int length = _array.Length;
        if (length == 0)
            return int.MinValue;

        int max = int.MinValue;
        fixed (int* ptr = _array)
        {
            // Граничные элементы
            if (length >= 1)
            {
                int val = ptr[0];
                if (!IsLocalMinOrMax(_array, 0))
                    max = Math.Max(max, val);
            }

            if (length >= 2)
            {
                int val = ptr[length - 1];
                if (!IsLocalMinOrMax(_array, length - 1))
                    max = Math.Max(max, val);
            }

            if (length < 3) return max;

            int i = 1;
            int end = length - 1;
            int endVectorized = i + ((end - i) & ~7);

            // Развернутый цикл с локальной переменной для избежания лишних обращений к памяти
            while (i < endVectorized)
            {
                int localMax = max;
                localMax = Math.Max(localMax, IsLocalMinOrMax(_array, i) ? int.MinValue : ptr[i]);
                localMax = Math.Max(localMax, IsLocalMinOrMax(_array, i + 1) ? int.MinValue : ptr[i + 1]);
                localMax = Math.Max(localMax, IsLocalMinOrMax(_array, i + 2) ? int.MinValue : ptr[i + 2]);
                localMax = Math.Max(localMax, IsLocalMinOrMax(_array, i + 3) ? int.MinValue : ptr[i + 3]);
                localMax = Math.Max(localMax, IsLocalMinOrMax(_array, i + 4) ? int.MinValue : ptr[i + 4]);
                localMax = Math.Max(localMax, IsLocalMinOrMax(_array, i + 5) ? int.MinValue : ptr[i + 5]);
                localMax = Math.Max(localMax, IsLocalMinOrMax(_array, i + 6) ? int.MinValue : ptr[i + 6]);
                localMax = Math.Max(localMax, IsLocalMinOrMax(_array, i + 7) ? int.MinValue : ptr[i + 7]);
                max = localMax;
                i += 8;
            }

            // Остаток
            while (i < end)
            {
                int val = ptr[i];
                if (!IsLocalMinOrMax(_array, i))
                    max = Math.Max(max, val);
                i++;
            }
        }

        return max;
    }

    //-------------------------------------------------------------------------------------
    //                                                                           Array_SIMD
    //-------------------------------------------------------------------------------------
    [Benchmark]
    public int Array_SIMD()
    {
        var array = _array;
        int length = array.Length;
        if (length < 3)
        {
            int max = int.MinValue;
            for (int j = 0; j < length; j++)
                if (!IsLocalMinOrMax(array, j))
                    max = Math.Max(max, array[j]);
            return max;
        }

        int vectorSize = Vector<int>.Count;
        var vMax = new Vector<int>(int.MinValue);
        int resultMax = int.MinValue;

        // Границы
        if (!IsLocalMinOrMax(array, 0))
            resultMax = Math.Max(resultMax, array[0]);

        if (!IsLocalMinOrMax(array, length - 1))
            resultMax = Math.Max(resultMax, array[length - 1]);

        int i = 1;
        int end = length - 1;

        var minVec = new Vector<int>(int.MinValue);

        // Основной SIMD-цикл
        for (; i <= end - vectorSize; i += vectorSize)
        {
            var current = new Vector<int>(array, i);
            var left = new Vector<int>(array, i - 1);
            var right = new Vector<int>(array, i + 1);

            var isMin = Vector.LessThan(current, left) & Vector.LessThan(current, right);
            var isMax = Vector.GreaterThan(current, left) & Vector.GreaterThan(current, right);
            var isExtreme = isMin | isMax;

            var candidate = Vector.ConditionalSelect(Vector.OnesComplement(isExtreme), current, minVec);
            vMax = Vector.Max(vMax, candidate);
        }

        // Более эффективное извлечение максимума из вектора
        for (int j = 0; j < vectorSize; j++)
            resultMax = Math.Max(resultMax, vMax[j]);

        // Остаток
        for (; i < end; i++)
        {
            int val = array[i];
            if (!IsLocalMinOrMax(array, i))
                resultMax = Math.Max(resultMax, val);
        }

        return resultMax;
    }

    //-------------------------------------------------------------------------------------
    //                                                                Array_SIMD_Intrinsics
    //-------------------------------------------------------------------------------------
    [Benchmark]
    public unsafe int Array_SIMD_Intrinsics()
    {
        if (!Avx2.IsSupported)
            throw new PlatformNotSupportedException("AVX2 not supported on this CPU");

        var array = _array;
        int length = array.Length;

        if (length < 3)
        {
            int max = int.MinValue;
            for (int i = 0; i < length; i++)
                if (!IsLocalMinOrMax(array, i))
                    max = Math.Max(max, array[i]);
            return max;
        }

        fixed (int* ptr = array)
        {
            int resultMax = int.MinValue;

            // Границы
            if (!IsLocalMinOrMax(array, 0))
                resultMax = Math.Max(resultMax, ptr[0]);

            if (!IsLocalMinOrMax(array, length - 1))
                resultMax = Math.Max(resultMax, ptr[length - 1]);

            Vector256<int> vMax = Vector256.Create(int.MinValue);
            Vector256<int> minValueVec = Vector256.Create(int.MinValue);
            int i = 1;
            int end = length - 1;
            int simdEnd = i + ((end - i) & ~7);

            for (; i < simdEnd; i += 8)
            {
                Vector256<int> current = Avx.LoadVector256(ptr + i);
                Vector256<int> left = Avx.LoadVector256(ptr + i - 1);
                Vector256<int> right = Avx.LoadVector256(ptr + i + 1);

                Vector256<int> isMin = Avx2.And(
                    Avx2.CompareGreaterThan(left, current),
                    Avx2.CompareGreaterThan(right, current)
                );

                Vector256<int> isMax = Avx2.And(
                    Avx2.CompareGreaterThan(current, left),
                    Avx2.CompareGreaterThan(current, right)
                );

                Vector256<int> isExtreme = Avx2.Or(isMin, isMax);
                Vector256<int> candidate = Avx2.BlendVariable(current, minValueVec, isExtreme);
                vMax = Avx2.Max(vMax, candidate);
            }

            // Упрощенное извлечение максимума из вектора
            int* temp = stackalloc int[8];
            Avx.Store(temp, vMax);

            for (int j = 0; j < 8; j++)
                resultMax = Math.Max(resultMax, temp[j]);

            // Остаток
            for (; i < end; i++)
            {
                int val = ptr[i];
                if (!IsLocalMinOrMax(array, i))
                    resultMax = Math.Max(resultMax, val);
            }

            return resultMax;
        }
    }
}