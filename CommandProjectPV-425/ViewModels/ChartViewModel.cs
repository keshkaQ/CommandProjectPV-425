using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;
using CommandProjectPV_425.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CommandProjectPV_425.ViewModels
{
    public class ChartViewModel : INotifyPropertyChanged
    {
        private readonly IChartService _chartService;
        private readonly IDataService _dataService;

        // Вспомогательный массив для цветов
        private readonly SKColor[] Colors =
        [
            SKColors.DodgerBlue, SKColors.Tomato, SKColors.MediumSeaGreen,
            SKColors.Gold, SKColors.SlateBlue, SKColors.Firebrick,
            SKColors.DarkCyan, SKColors.Orange, SKColors.Purple, SKColors.Teal
        ];

        public ISeries[] TimeSeries { get; private set; }
        public ISeries[] SpeedupSeries { get; private set; }
        public ISeries[] MethodStatsSeries { get; private set; }

        public Axis[] TimeXAxes { get; private set; }
        public Axis[] TimeYAxes { get; private set; }
        public Axis[] SpeedupXAxes { get; private set; }
        public Axis[] SpeedupYAxes { get; private set; }
        public Axis[] MethodStatsXAxes { get; private set; }
        public Axis[] MethodStatsYAxes { get; private set; }

        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public ChartViewModel(IChartService chartService, IDataService dataService)
        {
            _chartService = chartService;
            _dataService=dataService;

            InitializeCharts();
        }

        public ChartViewModel() : this(new ChartService(), new DataService())
        {
        }

        private void InitializeCharts()
        {
            // Инициализация пустых графиков
            TimeSeries = Array.Empty<ISeries>();
            SpeedupSeries = Array.Empty<ISeries>();

            MethodStatsSeries = Array.Empty<ISeries>();

            TimeXAxes = Array.Empty<Axis>();
            TimeYAxes = Array.Empty<Axis>();
            SpeedupXAxes = Array.Empty<Axis>();
            SpeedupYAxes = Array.Empty<Axis>();

            MethodStatsXAxes = Array.Empty<Axis>();
            MethodStatsYAxes = Array.Empty<Axis>();
        }

        public void UpdateCharts(List<string> labels, List<double> timeValues, List<double> speedupValues)
        {
            if (labels == null || timeValues == null || speedupValues == null ||
                labels.Count == 0 || timeValues.Count == 0 || speedupValues.Count == 0)
            {
                ClearCharts(); ;
                return;
            }

            UpdateTimeChart(labels, timeValues);
            UpdateSpeedupChart(labels, speedupValues);


            // Уведомляем об изменении всех свойств
            NotifyAllChartPropertiesChanged();
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

        private (Axis[] X, Axis[] Y) CreateAxes(
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

        public async Task LoadMethodStatisticsAsync()
        {
            try
            {
                // 1. Получение данных из БД
                var stats = await _dataService.GetAverageTimePerMethodAsync();

                // 2. Построение графика
                UpdateMethodsStatsChart(stats);
            }
            catch (Exception ex)
            {
                // Обработка ошибки загрузки данных
                MessageBox.Show($"Ошибка при загрузке статистики методов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                // Очистка графика при ошибке
                MethodStatsSeries = Array.Empty<ISeries>();
                OnPropertyChanged(nameof(MethodStatsSeries));
            }
        }

        private (ISeries[] Series, Axis[] XAxes, Axis[] YAxes) CreateColumnChart(
            List<string> labels,
            List<double> values,
            string xAxisName,
            string yAxisName,
            Func<double, string> yLabelFormatter,
            Func<int, double, string> tooltipFormatter)
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

            var (xAxes, yAxes) = CreateAxes(xAxisName, yAxisName, values, yLabelFormatter);
            return (seriesList.ToArray(), xAxes, yAxes);
        }

        public void UpdateMethodsStatsChart(List<MethodStatistic> stats)
        {
            if (stats == null || stats.Count == 0)
            {
                MethodStatsSeries = Array.Empty<ISeries>();
                MethodStatsXAxes = Array.Empty<Axis>();
                MethodStatsYAxes = Array.Empty<Axis>();
                OnPropertyChanged(nameof(MethodStatsSeries));
                return;
            }

            var seriesList = new List<ISeries>();
            var values = stats.Select(s => s.AverageTimeMs).ToList();
            var labels = stats.Select(s => s.MethodName).ToList();

            (MethodStatsSeries, MethodStatsXAxes, MethodStatsYAxes) = CreateColumnChart(
                labels,
                values,
                "Методы",
                "Среднее время выполнения",
                Helpers.TimeFormatter.FormatTimeShort,
                (index, val) => $"{labels[index]}\n{Helpers.TimeFormatter.FormatTime(val)}"
            );

            NotifyAllChartPropertiesChanged();
        }

        private void UpdateTimeChart(List<string> labels, List<double> values)
        {
            (TimeSeries, TimeXAxes, TimeYAxes) = CreateColumnChart(
                labels,
                values,
                "Методы",
                "Время выполнения",
                Helpers.TimeFormatter.FormatTimeShort,
                (index, val) => $"{labels[index]}\n{Helpers.TimeFormatter.FormatTime(val)}"
            );
        }

        private void UpdateSpeedupChart(List<string> labels, List<double> values)
        {
            (SpeedupSeries, SpeedupXAxes, SpeedupYAxes) = CreateColumnChart(
                labels,
                values,
                "Методы",
                "Ускорение (x)",
                v => v.ToString("0.##"),
                (index, val) => $"{labels[index]}\n{val:0.##}x"
            );
        }

        public void ClearCharts()
        {
            InitializeCharts();
            NotifyAllChartPropertiesChanged();
        }

        private void NotifyAllChartPropertiesChanged()
        {
            OnPropertyChanged(nameof(TimeSeries));
            OnPropertyChanged(nameof(SpeedupSeries));
            OnPropertyChanged(nameof(TimeXAxes));
            OnPropertyChanged(nameof(TimeYAxes));
            OnPropertyChanged(nameof(SpeedupXAxes));
            OnPropertyChanged(nameof(SpeedupYAxes));

            OnPropertyChanged(nameof(MethodStatsSeries));
            OnPropertyChanged(nameof(MethodStatsXAxes));
            OnPropertyChanged(nameof(MethodStatsYAxes));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}