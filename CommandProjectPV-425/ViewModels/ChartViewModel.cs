using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;
using CommandProjectPV_425.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CommandProjectPV_425.ViewModels
{
    public class ChartViewModel : INotifyPropertyChanged
    {
        private readonly IChartService _chartService;
        private ObservableCollection<BenchmarkResult> _results;

        public ISeries[] TimeSeries { get; private set; }
        public ISeries[] SpeedupSeries { get; private set; }

        public Axis[] TimeXAxes { get; private set; }
        public Axis[] TimeYAxes { get; private set; }
        public Axis[] SpeedupXAxes { get; private set; }
        public Axis[] SpeedupYAxes { get; private set; }

        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public ChartViewModel(IChartService chartService)
        {
            _chartService = chartService;
            InitializeCharts();
        }

        public ChartViewModel() : this(new ChartService())
        {
        }

        private void InitializeCharts()
        {
            TimeSeries = Array.Empty<ISeries>();
            SpeedupSeries = Array.Empty<ISeries>();

            TimeXAxes = Array.Empty<Axis>();
            TimeYAxes = Array.Empty<Axis>();
            SpeedupXAxes = Array.Empty<Axis>();
            SpeedupYAxes = Array.Empty<Axis>();
        }

        public void UpdateCharts(List<string> labels, List<double> timeValues, List<double> speedupValues)
        {
            if (labels == null || timeValues == null || speedupValues == null ||
                labels.Count == 0 || timeValues.Count == 0 || speedupValues.Count == 0)
            {
                InitializeCharts();
                OnPropertyChanged(nameof(TimeSeries));
                OnPropertyChanged(nameof(SpeedupSeries));
                OnPropertyChanged(nameof(TimeXAxes));
                OnPropertyChanged(nameof(TimeYAxes));
                OnPropertyChanged(nameof(SpeedupXAxes));
                OnPropertyChanged(nameof(SpeedupYAxes));
                return;
            }

            UpdateTimeChart(labels, timeValues);
            UpdateSpeedupChart(labels, speedupValues);

            OnPropertyChanged(nameof(TimeSeries));
            OnPropertyChanged(nameof(SpeedupSeries));
            OnPropertyChanged(nameof(TimeXAxes));
            OnPropertyChanged(nameof(TimeYAxes));
            OnPropertyChanged(nameof(SpeedupXAxes));
            OnPropertyChanged(nameof(SpeedupYAxes));
        }

        public void UpdateCharts(IEnumerable<BenchmarkResult> results)
        {
            if (results == null || !results.Any())
            {
                InitializeCharts();
                return;
            }

            var (labels, timeValues, speedupValues) = _chartService.PrepareChartData(results);
            UpdateCharts(labels, timeValues, speedupValues);
        }

        // Вспомогательный массив для цветов
        private readonly SKColor[] Colors =
        [
            SKColors.DodgerBlue, SKColors.Tomato, SKColors.MediumSeaGreen,
            SKColors.Gold, SKColors.SlateBlue, SKColors.Firebrick,
            SKColors.DarkCyan, SKColors.Orange, SKColors.Purple, SKColors.Teal
        ];

        private void UpdateTimeChart(List<string> labels, List<double> values)
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
                            return $"{labels[index]}\n{Helpers.TimeFormatter.FormatTime(point.Model.Y)}";
                        return "N/A";
                    }
                };

                seriesList.Add(series);
            }

            TimeSeries = seriesList.ToArray();

            double minValue = values.Min();
            double maxValue = values.Max();
            double padding = (maxValue - minValue) * 0.1;

            TimeXAxes =
            [
                new Axis
                {
                    Labeler = value => string.Empty,
                    Labels = null,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                    MinStep = 1,
                    Name = "Методы",
                    NamePaint = new SolidColorPaint(SKColors.Black)
                }
            ];

            TimeYAxes =
            [
                new Axis
                {
                    Labeler = value => Helpers.TimeFormatter.FormatTimeShort(value),
                    MinLimit = Math.Max(0, minValue - padding),
                    MaxLimit = maxValue + padding,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                    Name = "Время выполнения",
                    NamePaint = new SolidColorPaint(SKColors.Black)
                }
            ];
        }

        private void UpdateSpeedupChart(List<string> labels, List<double> speedupPercentages)
        {
            var seriesList = new List<ISeries>();

            for (int i = 0; i < speedupPercentages.Count; i++)
            {
                var singlePoint = new List<ObservablePoint> { new ObservablePoint(i, speedupPercentages[i]) };

                // Определяем цвет столбца в зависимости от значения
                SKColor barColor = speedupPercentages[i] switch
                {
                    > 0 => SKColors.MediumSeaGreen,  // Зеленый для положительного ускорения
                    < 0 => SKColors.Tomato,          // Красный для отрицательного ускорения
                    _ => SKColors.Gray               // Серый для нуля
                };

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
                        {
                            var percentage = point.Model.Y;
                            return $"{labels[index]}\n{FormatSpeedupForTooltip(percentage)}";
                        }
                        return "N/A";
                    }
                };

                seriesList.Add(series);
            }

            SpeedupSeries = seriesList.ToArray();

            // Для графика ускорения в процентах устанавливаем ось Y с процентами
            double minValue = speedupPercentages.Min();
            double maxValue = speedupPercentages.Max();
            double padding = Math.Max((maxValue - minValue) * 0.1, 10); // Минимальный отступ 10%

            SpeedupXAxes =
            [
                new Axis
                {
                    Labeler = value => string.Empty,
                    Labels = null,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                    MinStep = 1,
                    Name = "Методы",
                    NamePaint = new SolidColorPaint(SKColors.Black)
                }
            ];

            SpeedupYAxes =
            [
                new Axis
                {
                    Labeler = value => $"{value:F0}%", // Форматируем как проценты
                    MinLimit = minValue - padding,
                    MaxLimit = maxValue + padding,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                    Name = "Прирост производительности",
                    NamePaint = new SolidColorPaint(SKColors.Black)
                }
            ];
        }

        // Вспомогательный метод для форматирования ускорения в тултипе
        private string FormatSpeedupForTooltip(double? percentage)
        {
            if (percentage > 0)
                return $"+{percentage:F1}%";
            else if (percentage < 0)
                return $"{percentage:F1}%";
            else
                return "0%";
        }

        public void ClearCharts()
        {
            InitializeCharts();
            OnPropertyChanged(nameof(TimeSeries));
            OnPropertyChanged(nameof(SpeedupSeries));
            OnPropertyChanged(nameof(TimeXAxes));
            OnPropertyChanged(nameof(TimeYAxes));
            OnPropertyChanged(nameof(SpeedupXAxes));
            OnPropertyChanged(nameof(SpeedupYAxes));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}