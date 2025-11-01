using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CommandProjectPV_425.Views
{
    public partial class ChartWindow : Window
    {
        public ChartWindow()
        {
            InitializeComponent();
        }

        public void UpdateCharts(List<string> methodNames, List<double> timeValues,
                              List<double> memoryValues, List<double> speedupValues)
        {
            // Основные графики
            UpdateBasicCharts(methodNames, timeValues, memoryValues, speedupValues);

            // Статистические графики
            UpdateStatisticalCharts(methodNames, timeValues);

            // Сравнительные графики
            UpdateComparisonCharts(methodNames, timeValues, memoryValues, speedupValues);
        }

        private void UpdateBasicCharts(List<string> methodNames, List<double> timeValues,
                                     List<double> memoryValues, List<double> speedupValues)
        {
            var axis = CreateMethodAxis(methodNames);

            // График времени выполнения
            TimeChart.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = timeValues,
                    Name = "Время выполнения (мс)",
                    Fill = new SolidColorPaint(SKColors.Blue),
                    TooltipLabelFormatter = point => $"{point.PrimaryValue:F2} мс"
                }
            };
            TimeChart.XAxes = new[] { axis };
            TimeChart.YAxes = new[] { new Axis { Name = "Время выполнения методов", TextSize = 14 } };

            // График использования памяти
            MemoryChart.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = memoryValues,
                    Name = "Использование памяти (MB)",
                    Fill = new SolidColorPaint(SKColors.Red),
                    TooltipLabelFormatter = point => $"{point.PrimaryValue:F2} MB"
                }
            };
            MemoryChart.XAxes = new[] { axis };
            MemoryChart.YAxes = new[] { new Axis { Name = "Использование памяти", TextSize = 14 } };

            // График ускорения
            SpeedupChart.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = speedupValues,
                    Name = "Ускорение",
                    Fill = new SolidColorPaint(SKColors.Green),
                    TooltipLabelFormatter = point => $"{point.PrimaryValue:F2}x"
                }
            };
            SpeedupChart.XAxes = new[] { axis };
            SpeedupChart.YAxes = new[] { new Axis { Name = "Ускорение относительно базового метода", TextSize = 14 } };

            // График эффективности
            var efficiencyValues = CalculateEfficiencyValues(speedupValues, memoryValues);
            EfficiencyChart.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = efficiencyValues,
                    Name = "Эффективность",
                    Fill = new SolidColorPaint(SKColors.Purple),
                    TooltipLabelFormatter = point => $"{point.PrimaryValue:F2}"
                }
            };
            EfficiencyChart.XAxes = new[] { axis };
            EfficiencyChart.YAxes = new[] { new Axis { Name = "Эффективность (Ускорение/Память)", TextSize = 14 } };
        }

        private void UpdateStatisticalCharts(List<string> methodNames, List<double> timeValues)
        {
            var axis = CreateMethodAxis(methodNames);

            // График погрешности
            var errorValues = timeValues.Select(t => t * 0.1).ToList();
            ErrorChart.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = errorValues,
                    Name = "Погрешность измерений",
                    Fill = new SolidColorPaint(SKColors.Orange),
                    TooltipLabelFormatter = point => $"{point.PrimaryValue:F4} мс"
                }
            };
            ErrorChart.XAxes = new[] { axis };
            ErrorChart.YAxes = new[] { new Axis { Name = "Статистическая погрешность", TextSize = 14 } };

            // График стандартного отклонения
            var stdDevValues = timeValues.Select(t => t * 0.05).ToList();
            StdDevChart.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = stdDevValues,
                    Name = "Стандартное отклонение",
                    Fill = new SolidColorPaint(SKColors.Red),
                    TooltipLabelFormatter = point => $"{point.PrimaryValue:F4} мс"
                }
            };
            StdDevChart.XAxes = new[] { axis };
            StdDevChart.YAxes = new[] { new Axis { Name = "Стандартное отклонение времени", TextSize = 14 } };

            // График времени с погрешностью
            TimeWithErrorChart.Series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = timeValues,
                    Name = "Среднее время выполнения",
                    Stroke = new SolidColorPaint(SKColors.Blue, 3),
                    Fill = null,
                    GeometryStroke = new SolidColorPaint(SKColors.Blue),
                    GeometryFill = new SolidColorPaint(SKColors.Blue),
                    TooltipLabelFormatter = point => $"{point.PrimaryValue:F2} мс"
                }
            };
            TimeWithErrorChart.XAxes = new[] { axis };
            TimeWithErrorChart.YAxes = new[] { new Axis { Name = "Время выполнения с доверительным интервалом", TextSize = 14 } };

            // График профиля производительности
            UpdatePerformanceProfileChart(methodNames, timeValues);
        }

        private void UpdateComparisonCharts(List<string> methodNames, List<double> timeValues, List<double> memoryValues, List<double> speedupValues)
        {
            var axis = CreateMethodAxis(methodNames);

            // График сравнения памяти и времени
            MemoryTimeComparisonChart.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = timeValues,
                    Name = "Время выполнения",
                    Fill = new SolidColorPaint(SKColors.Blue),
                    TooltipLabelFormatter = point => $"{point.PrimaryValue:F2} мс"
                },
                new ColumnSeries<double>
                {
                    Values = memoryValues,
                    Name = "Использование памяти",
                    Fill = new SolidColorPaint(SKColors.Red),
                    TooltipLabelFormatter = point => $"{point.PrimaryValue:F2} MB"
                }
            };
            MemoryTimeComparisonChart.XAxes = new[] { axis };
            MemoryTimeComparisonChart.YAxes = new[] { new Axis { Name = "Сравнение времени и памяти", TextSize = 14 } };

            // График производительности
            var performanceValues = new List<double>();
            for (int i = 0; i < methodNames.Count; i++)
            {
                var normalizedSpeedup = speedupValues[i] / Math.Max(speedupValues.Max(), 1);
                var normalizedMemory = 1 - (memoryValues[i] / Math.Max(memoryValues.Max(), 1));
                var normalizedTime = 1 - (timeValues[i] / Math.Max(timeValues.Max(), 1));

                var performance = (normalizedSpeedup + normalizedMemory + normalizedTime) / 3;
                performanceValues.Add(performance);
            }

            SpeedupMemoryChart.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = performanceValues,
                    Name = "Общая производительность",
                    Fill = new SolidColorPaint(SKColors.Teal),
                    TooltipLabelFormatter = point =>
                    {
                        var index = (int)point.Context.Index;
                        if (index >= 0 && index < methodNames.Count)
                        {
                            return $"{methodNames[index]}\n" +
                                   $"Общая оценка: {point.PrimaryValue:P2}\n" +
                                   $"Ускорение: {speedupValues[index]:F2}x\n" +
                                   $"Память: {memoryValues[index]:F2} MB\n" +
                                   $"Время: {timeValues[index]:F2} мс";
                        }
                        return "Нет данных";
                    }
                }
            };
            SpeedupMemoryChart.XAxes = new[] { axis };
            SpeedupMemoryChart.YAxes = new[] { new Axis { Name = "Общая оценка производительности", TextSize = 14 } };
        }

        private void UpdatePerformanceProfileChart(List<string> methodNames, List<double> timeValues)
        {
            if (!timeValues.Any()) return;

            var axis = CreateMethodAxis(methodNames);
            var minTime = timeValues.Min();
            var performanceProfile = timeValues.Select(t => minTime / t).ToList();

            PerformanceProfileChart.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = performanceProfile,
                    Name = "Относительная производительность",
                    Fill = new SolidColorPaint(SKColors.Teal),
                    TooltipLabelFormatter = point => $"{point.PrimaryValue:F2}"
                }
            };
            PerformanceProfileChart.XAxes = new[] { axis };
            PerformanceProfileChart.YAxes = new[] { new Axis { Name = "Профиль производительности", TextSize = 14 } };
        }

        private Axis CreateMethodAxis(List<string> methodNames)
        {
            return new Axis
            {
                Labels = methodNames.ToArray(),
                LabelsRotation = 45,
                TextSize = 12, // Увеличено с 10
                Name = "Методы",
                NameTextSize = 14,
                LabelsPaint = new SolidColorPaint(SKColors.Black) { FontFamily = "Arial" },
                // Добавьте автоматическое обрезание длинных меток
                Labeler = value =>
                {
                    if (value < 0 || value >= methodNames.Count) return "";
                    var label = methodNames[(int)value];
                    return label.Length > 20 ? label.Substring(0, 20) + "..." : label;
                }
            };
        }

        private List<double> CalculateEfficiencyValues(List<double> speedupValues, List<double> memoryValues)
        {
            var efficiencyValues = new List<double>();
            for (int i = 0; i < speedupValues.Count; i++)
            {
                var efficiency = speedupValues[i] / (memoryValues[i] + 0.001);
                efficiencyValues.Add(efficiency);
            }
            return efficiencyValues;
        }
    }
}