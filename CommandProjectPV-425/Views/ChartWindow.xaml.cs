using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Defaults;
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

        public void UpdateCharts(List<string> methodNames, List<double> timeValues, List<double> speedupValues)
        {

            // Обновляем графики
            UpdateTimeChart(methodNames, timeValues);
            UpdateSpeedupChart(methodNames, speedupValues);
        }

        private void UpdateTimeChart(List<string> labels, List<double> values)
        {
            if (labels == null || values == null || labels.Count == 0 || values.Count == 0)
                return;

            // Автоматическое определение пределов оси Y
            double minValue = values.Min();
            double maxValue = values.Max();
            double padding = (maxValue - minValue) * 0.1; // 10% отступ

            var points = new List<ObservablePoint>();
            for (int i = 0; i < values.Count; i++)
            {
                points.Add(new ObservablePoint(i, values[i]));
            }

            var series = new LineSeries<ObservablePoint>
            {
                Values = points,
                Stroke = new SolidColorPaint(SKColors.Blue, 3),
                Fill = null,
                GeometryStroke = new SolidColorPaint(SKColors.Blue, 2),
                GeometryFill = new SolidColorPaint(SKColors.White),
                GeometrySize = 10,
                LineSmoothness = 0,
                TooltipLabelFormatter = point =>
                {
                    var index = (int)point.Model.X;
                    if (index >= 0 && index < labels.Count)
                        return $"{labels[index]}\n{FormatTime(point.Model.Y)}";
                    return "N/A";
                }
            };

            TimeChart.Series = new[] { series };

            TimeChart.XAxes = new[]
            {
        new Axis
        {
            Labels = labels.ToArray(),
            LabelsRotation = 45,
            TextSize = 10,
            LabelsPaint = new SolidColorPaint(SKColors.Black),
            SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
            ForceStepToMin = true,
            MinStep = 1
        }
    };

            TimeChart.YAxes = new[]
            {
        new Axis
        {
            Labeler = value => FormatTimeShort(value),
            MinLimit = Math.Max(0, minValue - padding), // Автоматические пределы
            MaxLimit = maxValue + padding,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1)
        }
    };
        }
        private void UpdateSpeedupChart(List<string> labels, List<double> values)
        {
            if (labels == null || values == null || labels.Count == 0 || values.Count == 0)
                return;

            // Автоматическое определение пределов
            double minValue = values.Min();
            double maxValue = values.Max();
            double padding = (maxValue - minValue) * 0.1;

            var points = new List<ObservablePoint>();
            for (int i = 0; i < values.Count; i++)
            {
                points.Add(new ObservablePoint(i, values[i]));
            }

            var series = new LineSeries<ObservablePoint>
            {
                Values = points,
                Stroke = new SolidColorPaint(SKColors.Green, 3),
                Fill = null,
                GeometryStroke = new SolidColorPaint(SKColors.Green, 2),
                GeometryFill = new SolidColorPaint(SKColors.White),
                GeometrySize = 10,
                LineSmoothness = 0,
                TooltipLabelFormatter = point =>
                {
                    var index = (int)point.Model.X;
                    if (index >= 0 && index < labels.Count)
                        return $"{labels[index]}\n{point.Model.Y:F2}x";
                    return "N/A";
                }
            };

            SpeedupChart.Series = new[] { series };

            SpeedupChart.XAxes = new[]
            {
        new Axis
        {
            Labels = labels.ToArray(),
            LabelsRotation = 45,
            TextSize = 10,
            LabelsPaint = new SolidColorPaint(SKColors.Black),
            SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
            ForceStepToMin = true,
            MinStep = 1
        }
    };

            SpeedupChart.YAxes = new[]
            {
        new Axis
        {
            MinLimit = Math.Max(0, minValue - padding),
            MaxLimit = maxValue + padding,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1)
        }
    };
        }

        private string FormatTime(double? timeMs)
        {
            if (timeMs < 0.001)
                return $"{(timeMs * 1_000_000):F2} ns";
            else if (timeMs < 0.1)
                return $"{(timeMs * 1000):F2} μs";
            else if (timeMs < 1000)
                return $"{timeMs:F2} ms";
            else
                return $"{(timeMs / 1000):F2} s";
        }

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
    }
}