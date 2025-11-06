using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;

namespace CommandProjectPV_425.Services
{
    public class ChartService : IChartService
    {
        private const int MAX_COLUMN_CHARTS = 9;

        public (List<string> labels, List<double> timeValues, List<double> speedupValues) PrepareChartData(IEnumerable<BenchmarkResult> results)
        {
            var successfulResults = results
                .Where(r => r.ExecutionTime != "Failed")    // только успешные результаты
                .OrderBy(r => r.TaskType)                   // Сортировка по типу задачи
                .ThenBy(r => r.MethodName)                  // Затем по имени метода
                .ToList();

            var labels = new List<string>();                //  подписи на оси X (методы + задачи)
            var timeValues = new List<double>();            // значения для графика времени выполнения(ось Y)
            var speedupValues = new List<double>();

            int count = results.Count();

            if (count > MAX_COLUMN_CHARTS)
            {
                var statistics = successfulResults
                    .GroupBy(r => r.MethodName)
                    .Select(g =>
                    {
                        // Берём средние значения для каждого метода (MethodName уникален)
                        double averageTime = g.Average(r => Helpers.DataParser.ParseTimeToMs(r.ExecutionTime));
                        double averageSpeed = g.Average(r => Helpers.DataParser.ParseSpeedup(r.Speedup));

                        return new MethodStatistic
                        {
                            MethodName = g.Key,
                            AverageTimeMs = averageTime,
                            AverageSpeedup = averageSpeed
                        };
                    })
                    .Where(s => s.AverageTimeMs > 0) // Удаляем методы, у которых нет успешных результатов
                    .OrderBy(s => s.AverageTimeMs)
                    .ToList();

                foreach (var s in statistics)
                {
                    labels.Add(s.MethodName);
                    timeValues.Add(s.AverageTimeMs);
                    speedupValues.Add(s.AverageSpeedup);
                }
            }
            else
            {
                foreach (var result in successfulResults)
                {
                    // сокращаем имя метода и задачи
                    var task = result.TaskType;
                    //var task = result.TaskType.Length > 15 ?
                    //    result.TaskType.Substring(0, 15) + "..." 
                    //    : result.TaskType;
                    var method = result.MethodName;

                    // конвертируем строковое время и ускорение в миллисекунды
                    labels.Add($"{task}\n{method}");
                    timeValues.Add(Helpers.DataParser.ParseTimeToMs(result.ExecutionTime));
                    speedupValues.Add(Helpers.DataParser.ParseSpeedup(result.Speedup));
                }
            }

            return (labels, timeValues, speedupValues);
        }
    }
}
