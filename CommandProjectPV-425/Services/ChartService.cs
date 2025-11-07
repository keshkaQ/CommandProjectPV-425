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
        SKColors.DarkCyan, SKColors.Orange, SKColors.Purple, SKColors.Teal
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
                    // сокращаем имя метода и задачи
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

        //public (ISeries[] Series, Axis[] XAxes, Axis[] YAxes) CreateColumnChart(
        //    List<string> labels,
        //    List<double> values,
        //    string xAxisName,
        //    string yAxisName,
        //    Func<double, string> yLabelFormatter,
        //    Func<int, double, string> tooltipFormatter)
        //{
        //    var seriesList = new List<ISeries>();

        //    for (int i = 0; i < values.Count; i++)
        //    {
        //        var singlePoint = new List<ObservablePoint> { new ObservablePoint(i, values[i]) };
        //        var barColor = Colors[i % Colors.Length];

        //        var series = new ColumnSeries<ObservablePoint>
        //        {
        //            Values = singlePoint,
        //            Name = labels[i].Replace("\n", "-"),
        //            MaxBarWidth = 30,
        //            Fill = new SolidColorPaint(barColor),
        //            Stroke = null,
        //            TooltipLabelFormatter = point =>
        //            {
        //                var index = (int)point.Model.X;
        //                if (index >= 0 && index < labels.Count)
        //                    return tooltipFormatter(index, (double)point.Model.Y);
        //                return "N/A";
        //            }
        //        };

        //        seriesList.Add(series);
        //    }

        //    var (xAxes, yAxes) = CreateAxes(xAxisName, yAxisName, values, yLabelFormatter);
        //    return (seriesList.ToArray(), xAxes, yAxes);
        //}

        public (ISeries[] Series, Axis[] XAxes, Axis[] YAxes) CreateColumnChart(
    List<string> labels,
    List<double> values,
    string xAxisName,
    string yAxisName,
    Func<double, string> yLabelFormatter,
    Func<int, double, string> tooltipFormatter,
    bool isSpeedupChart = false) // ✅ Добавляем параметр для графика ускорения
        {
            var seriesList = new List<ISeries>();

            for (int i = 0; i < values.Count; i++)
            {
                var singlePoint = new List<ObservablePoint> { new ObservablePoint(i, values[i]) };

                // ✅ Для графика ускорения используем цветовую кодировку
                SKColor barColor;
                if (isSpeedupChart)
                {
                    barColor = values[i] switch
                    {
                        > 0 => SKColors.MediumSeaGreen,  // Зеленый для положительного ускорения
                        < 0 => SKColors.Tomato,          // Красный для отрицательного ускорения
                        _ => SKColors.Gray               // Серый для нуля
                    };
                }
                else
                {
                    barColor = Colors[i % Colors.Length]; // Обычные цвета для других графиков
                }

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

            // ✅ Для графика ускорения включаем ноль в диапазон
            if (isSpeedupChart)
            {
                minValue = Math.Min(minValue, 0) - Math.Abs(padding);
                maxValue = Math.Max(maxValue, 0) + Math.Abs(padding);
            }
            else
            {
                minValue = Math.Max(0, minValue - padding);
                maxValue = maxValue + padding;
            }

            var xAxes = new[]
            {
        new Axis
        {
            Labels = values.Select((v, i) => i.ToString()).ToArray(), // или labels если нужно
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

        public (Axis[] X, Axis[] Y) CreateAxes(
            string xName,
            string yName,
            List<double> values,
            Func<double, string> yLabelFormatter)
        {
            // Автоматическое определение пределов оси Y
            double minValue = values.Min();
            double maxValue = values.Max();
            double padding = (maxValue - minValue) * 0.1;

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
                MinLimit =Math.Max(0, minValue - padding),
                MaxLimit = maxValue + padding,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                Name = yName,
                NamePaint = new SolidColorPaint(SKColors.Black)
            }
        };

            return (xAxes, yAxes);
        }
    }
}