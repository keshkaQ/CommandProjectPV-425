using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CommandProjectPV_425.Services
{
    public class ChartService : IChartService
    {
        private const int MAX_COLUMN_CHARTS = 9;

        // Вспомогательный массив для цветов
        private readonly SKColor[] Colors =
        [
            SKColors.DodgerBlue, SKColors.Tomato, SKColors.MediumSeaGreen,
        SKColors.Gold, SKColors.SlateBlue, SKColors.Firebrick,
        SKColors.DarkCyan, SKColors.Orange, SKColors.Purple, SKColors.Teal,
        ];

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
                    var task = result.TaskType;
                    var method = result.MethodName;

                    // конвертируем строковое время и ускорение в миллисекунды
                    labels.Add($"{task}\n{method}");
                    timeValues.Add(Helpers.DataParser.ParseTimeToMs(result.ExecutionTime));
                    speedupValues.Add(Helpers.DataParser.ParseSpeedup(result.Speedup));
                }
            }

            return (labels, timeValues, speedupValues);
        }

        public (ISeries[] Series, Axis[] XAxes, Axis[] YAxes) CreateColumnChart(
            List<string> labels,
            List<double> values,
            string xAxisName,
            string yAxisName,
            Func<double, string> yLabelFormatter,
            Func<int, double, string> tooltipFormatter,
            bool isSpeedupChart = false)
        {
            var seriesList = new List<ISeries>();

            for (int i = 0; i < values.Count; i++)
            {
                var singlePoint = new List<ObservablePoint> { new ObservablePoint(i, values[i]) };
                var barColor = Colors[i % Colors.Length];

                var series = new ColumnSeries<ObservablePoint>
                {
                    Values = singlePoint,
                    Name = labels[i].Replace("\n", "-"),
                    MaxBarWidth = 30,
                    Fill = new SolidColorPaint(barColor),
                    Stroke = null,
                    TooltipLabelFormatter = point =>
                    {
                        var index = (int)point.Model.X;
                        if (index >= 0 && index < labels.Count)
                            return tooltipFormatter(index, (double)point.Model.Y);
                        return "N/A";
                    }
                };

                seriesList.Add(series);
            }

            var (xAxes, yAxes) = CreateAxes(xAxisName, yAxisName, values, yLabelFormatter, isSpeedupChart);
            return (seriesList.ToArray(), xAxes, yAxes);
        }

        public (Axis[] X, Axis[] Y) CreateAxes(
            string xName,
            string yName,
            List<double> values,
            Func<double, string> yLabelFormatter,
            bool isSpeedupChart = false) // ✅ Добавляем параметр для графика ускорения
        {
            // Автоматическое определение пределов оси Y
            double minValue = values.Min();
            double maxValue = values.Max();
            double padding = (maxValue - minValue) * 0.1;

            // ✅ Для графика ускорения включаем 1.0 в диапазон и центрируем его
            if (isSpeedupChart)
            {
                // 1. Включаем 1.0 (100%) в минимальное и максимальное значения
                minValue = Math.Min(minValue, 1.0);
                maxValue = Math.Max(maxValue, 1.0);

                // 2. Определяем максимальное отклонение от 1.0
                double maxDeviation = Math.Max(
                    Math.Abs(maxValue - 1.0),
                    Math.Abs(minValue - 1.0));

                // 3. Устанавливаем симметричный диапазон относительно 1.0
                // (с учетом небольшого отступа)
                double symPadding = maxDeviation + Math.Abs(maxDeviation * 0.1);

                minValue = 1.0 - symPadding;
                maxValue = 1.0 + symPadding;

                //minValue = Math.Max(0, minValue - padding);
                //maxValue = maxValue + padding;

            }
            else
            {
                // Оригинальная логика для обычного графика (включает 0)
                minValue = Math.Max(0, minValue - padding);
                maxValue = maxValue + padding;
            }

            var xAxes = new[]
            {
                new Axis
                {
                    // Установка Labeler в функцию, возвращающую пустую строку
                    Labeler = value => string.Empty,
                    Labels = null,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                    MinStep = 1,
                    Name = xName,
                    NamePaint = new SolidColorPaint(SKColors.Black)
                }
            };

            var yAxes = new[]
            {
                new Axis
                {
                    Labeler = yLabelFormatter,
                    MinLimit = minValue,
                    MaxLimit = maxValue,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                    Name = yName,
                    NamePaint = new SolidColorPaint(SKColors.Black)
                }
            };

            return (xAxes, yAxes);
        }
        public string GetTaskNameDescription(string taskName)
        {
            return taskName switch
            {
                "Count Numbers Above Average" => "Подсчёт количества элементов, превышающих среднее значение массива",
                "Divisible Three or Five" => "Подсчёт чисел, делящихся одновременно на 3 и 5 (кратные 15)",
                "Find Prime Numbers" => "Подсчёт количества простых чисел в массиве",
                "Maximum Of Non Extreme Elements" => "Поиск максимального значения среди элементов, не являющихся локальными экстремумами",
                "Max Frequency Of Elements" => "Поиск максимальной частоты повторения любого элемента",
                _ => null
            };
        }

        public List<MethodStatistic> CalculateAverageTimePerMethod(IEnumerable<BenchmarkResult> results)
        {
            var statistics = results
                .GroupBy(r => r.MethodName)
                .Select(g =>
                {
                    var validResults = g.Where(r => r.ExecutionTime != "Failed");

                    var averageTime = validResults.Any()
                        ? validResults.Average(r => Helpers.DataParser.ParseTimeToMs(r.ExecutionTime))
                        : 0.0;

                    return new MethodStatistic
                    {
                        MethodName = g.Key,
                        AverageTimeMs = averageTime,
                        AverageSpeedup = 0.0
                    };
                })
                .Where(s => s.AverageTimeMs > 0)
                .OrderBy(s => s.MethodName)
                .ToList();

            return statistics;
        }
    }
}

public class MethodStatistic
{
    public string MethodName { get; set; }
    public double AverageTimeMs { get; set; }
    public double AverageSpeedup { get; set; }
}