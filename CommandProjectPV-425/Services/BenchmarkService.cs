using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;
using CommandProjectPV_425.Tests;
using System.Globalization;

namespace CommandProjectPV_425.Services
{
    public class BenchmarkService : IBenchmarkService
    {
        public async Task<List<BenchmarkResult>> RunBenchmarkAsync(string taskType, int size)
        {
            var benchmarkType = GetBenchmarkType(taskType);                         // получаем тип задачи по названию бенчмарка
            if (benchmarkType == null)
                throw new ArgumentException($"Неизвестный тип задачи: {taskType}");

            SetBenchmarkSize(benchmarkType, size);                                  // устанавливаем размер


            // настраиваем конфигурацию BenchmarkDotNet:
            // базовая конфигурация
            // отключаем валидацию оптимизаций (для более стабильных результатов)
            // добавляем Job с минимальными настройками для быстрого выполнения
            var config = ManualConfig
                .Create(DefaultConfig.Instance)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddJob(Job.Dry.WithWarmupCount(1).WithIterationCount(1));

            // запускаем бенчмарк в отдельном потоке чтобы не блокировать UI
            return await Task.Run(() =>
            {
                var summary = BenchmarkRunner.Run(benchmarkType,config);
                return ProcessBenchmarkSummary(summary, taskType, size);
            });
        }

        public void SetBenchmarkSize(Type benchmarkType, int size)
        {
            // устанавливаем размер через статические свойства соответствующих классов
            if (benchmarkType == typeof(CountAboveAverage))
                CountAboveAverage.Size = size;
            else if (benchmarkType == typeof(DivisibleThreeOrFive))
                DivisibleThreeOrFive.Size = size;
            else if (benchmarkType == typeof(FindPrimeNumbers))
                FindPrimeNumbers.Size = size;
            else if (benchmarkType == typeof(MaximumOfNonExtremeElements))
                MaximumOfNonExtremeElements.Size = size;
            else if (benchmarkType == typeof(MaxFrequencyOfElements))
                MaxFrequencyOfElements.Size = size;
        }

        private Type GetBenchmarkType(string taskType)
        {
            return taskType switch
            {
                "Count Numbers Above Average" => typeof(CountAboveAverage),
                "Divisible Three or Five" => typeof(DivisibleThreeOrFive),
                "Find Prime Numbers" => typeof(FindPrimeNumbers),
                "Maximum Of Non Extreme Elements" => typeof(MaximumOfNonExtremeElements),
                "Max Frequency Of Elements" => typeof(MaxFrequencyOfElements),
                _ => null
            };
        }

        private List<BenchmarkResult> ProcessBenchmarkSummary(Summary summary, string taskType, int dataSize)
        {
            var results = new List<BenchmarkResult>();

            // Находим базовый метод [Benchmark(Baseline = true)] для расчета ускорения
            var baselineReport = summary.Reports.FirstOrDefault(r => r.BenchmarkCase.Descriptor.Baseline);
            double baselineTimeNs = baselineReport?.ResultStatistics?.Mean ?? 0;

            // Обрабатываем каждый метод-бенчмарк
            foreach (var report in summary.Reports)
            {
                var benchmarkCase = report.BenchmarkCase;

                // Меняем имя для отображения в DataGrid
                var methodName = FormatMethodName(benchmarkCase.Descriptor.WorkloadMethod.Name);

                // Получаем среднее время выполнения в наносекундах
                var meanTimeNs = report.ResultStatistics?.Mean ?? 0;

                // Рассчитываем ускорение относительно базового метода
                double speedup = 1.0;
                if (baselineTimeNs > 0 && meanTimeNs > 0)
                {
                    speedup = baselineTimeNs / meanTimeNs;
                }

                var speedupFormatted = FormatSpeedup(speedup);

                // Создаем объект результата со всей информацией
                results.Add(new BenchmarkResult
                {
                    Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Неизвестно",
                    CoreCount = Environment.ProcessorCount,
                    TaskType = taskType,
                    DataSize = dataSize,
                    MethodName = methodName,
                    ExecutionTime = Helpers.TimeFormatter.FormatTimeUs(meanTimeNs / 1000),
                    Speedup = speedupFormatted,
                    Timestamp = DateTime.Now,
                    RawTimes = new List<double> { meanTimeNs / 1000 }
                });
            }

            return results.OrderBy(r =>
            {
                var timeStr = r.ExecutionTime.Replace(" ms", "").Replace(",", ".");
                if (double.TryParse(timeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double timeMs))
                    return timeMs;
                return double.MaxValue;
            }).ToList();
        }
        // форматируем имена методов в названия для DataGrid
        public string FormatMethodName(string methodName)
        {
            return methodName switch
            {
                "Array_For" => "Array For",
                "Array_PLINQ" => "Array PLINQ",
                "Parallel_For" => "Parallel For",
                "Parallel_Partitioner" => "Parallel Partitioner",
                "Parallel_Invoke" => "Parallel Invoke",
                "Tasks_Run" => "Tasks Run",
                "Array_SIMD" => "Array SIMD",
                "Array_SIMD_Intrinsics" => "Array SIMD Intrinsics",
                "Array_Unsafe" => "Array Unsafe",
                _ => methodName
            };
        }
        // форматируем числовое значение ускорения в строку с нужной точностью
        private string FormatSpeedup(double speedup)
        {
            double percentage = (speedup - 1.0) * 100;

            return percentage switch
            {
                > 100 => $"{percentage:F0}%",    
                > 50 => $"{percentage:F0}%",     
                > 10 => $"{percentage:F1}%",     
                > 0 => $"{percentage:F1}%",      
                0 => "0%",                        
                > -10 => $"{percentage:F1}%",      
                > -50 => $"{percentage:F1}%",      
                _ => $"{percentage:F0}%"         
            };
        }
    }
}
