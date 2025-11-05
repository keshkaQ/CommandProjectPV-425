using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;

namespace CommandProjectPV_425.Services
{
    public class ChartService : IChartService
    {
        public (List<string> labels, List<double> timeValues, List<double> speedupValues) PrepareChartData(IEnumerable<BenchmarkResult> results)
        {
            
            var successfulResults = results
                .Where(r => r.ExecutionTime != "Failed")    // только успешные результаты
                .OrderBy(r => r.TaskType)                   // Сортировка по типу задачи
                .ThenBy(r => r.MethodName)                  // Затем по имени метода
                .ToList();

            var labels = new List<string>();                //  подписи на оси X (методы + задачи)
            var timeValues = new List<double>();            // значения для графика времени выполнения(ось Y)
            var speedupValues = new List<double>();         // значения для графика ускорения (ось Y)

            foreach (var result in successfulResults)
            {
                // сокращаем имя метода и задачи
                var task = result.TaskType.Length > 15 ?
                    result.TaskType.Substring(0, 15) + "..." 
                    : result.TaskType;
                var method = result.MethodName.Length > 10 ?
                    result.MethodName.Substring(0, 10) + "..." 
                    : result.MethodName;
                
                // конвертируем строковое время и ускорение в миллисекунды
                labels.Add($"{task}\n{method}");
                timeValues.Add(Helpers.DataParser.ParseTimeToMs(result.ExecutionTime));
                speedupValues.Add(Helpers.DataParser.ParseSpeedup(result.Speedup));
            }

            return (labels, timeValues, speedupValues);
        }
    }
}
