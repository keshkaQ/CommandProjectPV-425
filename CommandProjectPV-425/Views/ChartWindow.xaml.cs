using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
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
            // Обновляем только графики времени и ускорения
            UpdateTimeAndSpeedupCharts(methodNames, timeValues, speedupValues);
        }

        private void UpdateTimeAndSpeedupCharts(List<string> methodNames, List<double> timeValues, List<double> speedupValues)
        {
            var axis = CreateMethodAxis(methodNames);

            TimeChart.Series =
            [
                new ColumnSeries<double>
                {
                    Values = timeValues,
                    Name = "Время выполнения",
                    Fill = new SolidColorPaint(SKColors.Blue),
                    TooltipLabelFormatter = point => FormatTime(point.PrimaryValue)
                }
            ];

            TimeChart.XAxes = [axis];
            TimeChart.YAxes =
            [
                new Axis
                {
                    Name = "Время выполнения",
                    TextSize = 12,
                    NameTextSize = 14,
                    Labeler = value => FormatTimeShort(value)
                }
            ];

            // График ускорения
            SpeedupChart.Series =
            [
                new ColumnSeries<double>
                {
                    Name = "Ускорение",
                    Fill = new SolidColorPaint(SKColors.Green),
                    TooltipLabelFormatter = point => $"{point.PrimaryValue:F2}x"
                }
            ];

            SpeedupChart.XAxes = [axis];
            SpeedupChart.YAxes =
            [
                new Axis
                {
                    Name = "Ускорение (x)",
                    TextSize = 12,
                    NameTextSize = 14
                }
            ];
        }

        // Метод для форматирования времени в подсказках
        private string FormatTime(double timeMs)
        {
            if (timeMs < 0.001) // Наносекунды
                return $"{(timeMs * 1_000_000):F2} ns";
            else if (timeMs < 0.1) // Микросекунды
                return $"{(timeMs * 1000):F2} μs";
            else if (timeMs < 1000) // Миллисекунды
                return $"{timeMs:F2} ms";
            else // Секунды
                return $"{(timeMs / 1000):F2} s";
        }

        // Метод для форматирования времени на оси (укороченная версия)
        private string FormatTimeShort(double timeMs)
        {
            if (timeMs < 0.001)
                return $"{(timeMs * 1_000_000):F0} ns";
            else if (timeMs < 0.1)
                return $"{(timeMs * 1000):F0} μs";
            else if (timeMs < 1000)
                return $"{timeMs:F0} ms";
            else
                return $"{(timeMs / 1000):F1} s";
        }

        private Axis CreateMethodAxis(List<string> methodNames)
        {
            return new Axis
            {
                Labels = methodNames.ToArray(),
                LabelsRotation = 45,
                TextSize = 12,
                Name = "Методы",
                NameTextSize = 14,
                LabelsPaint = new SolidColorPaint(SKColors.Black) { FontFamily = "Arial" },
                Labeler = value =>
                {
                    if (value < 0 || value >= methodNames.Count) return "";
                    var label = methodNames[(int)value];
                    return label.Length > 20 ? label.Substring(0, 20) + "..." : label;
                }
            };
        }
    }
}