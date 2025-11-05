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
using System.Windows.Media;

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
            // Инициализация пустых графиков
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

            // Уведомляем об изменении всех свойств
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
            // Список для хранения всех серий (по одной на каждый столбец)
            var seriesList = new List<ISeries>();

            for (int i = 0; i < values.Count; i++)
            {
                // 1. Создаем список точек, содержащий только одну точку (для одного столбца)
                var singlePoint = new List<ObservablePoint> { new ObservablePoint(i, values[i]) };

                // 2. Определяем цвет для этого столбца
                var barColor = Colors[i % Colors.Length];

                // 3. Создаем новую ColumnSeries для этого столбца
                var series = new ColumnSeries<ObservablePoint>
                {
                    Values = singlePoint,
                    Name = labels[i].Replace("\n", " "), // Используем метку как имя серии
                    MaxBarWidth = 30,

                    // Задаем цвет заливки для этой серии
                    Fill = new SolidColorPaint(barColor),
                    Stroke = null,

                    // Настраиваем TooltipLabelFormatter для отображения полной информации
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

            // Присваиваем массив серий
            TimeSeries = seriesList.ToArray();

            // Автоматическое определение пределов оси Y
            double minValue = values.Min();
            double maxValue = values.Max();
            double padding = (maxValue - minValue) * 0.1;

            TimeXAxes =
            [
                new Axis
                {
                    // Установка Labeler в функцию, возвращающую пустую строку
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
                    MinLimit =Math.Max(0, minValue - padding),
                    MaxLimit = maxValue + padding,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                    Name = "Время выполнения",
                    NamePaint = new SolidColorPaint(SKColors.Black)
                }
            ];
        }

        private void UpdateSpeedupChart(List<string> labels, List<double> values)
        {
            // Список для хранения всех серий (по одной на каждый столбец)
            var seriesList = new List<ISeries>();

            for (int i = 0; i < values.Count; i++)
            {
                // 1. Создаем список точек, содержащий только одну точку (для одного столбца)
                var singlePoint = new List<ObservablePoint> { new ObservablePoint(i, values[i]) };

                // 2. Определяем цвет для этого столбца
                var barColor = Colors[i % Colors.Length];

                // 3. Создаем новую ColumnSeries для этого столбца
                var series = new ColumnSeries<ObservablePoint>
                {
                    Values = singlePoint,
                    Name = labels[i].Replace("\n", " "), // Используем метку как имя серии
                    MaxBarWidth = 30,

                    // Задаем цвет заливки для этой серии
                    Fill = new SolidColorPaint(barColor),
                    Stroke = null,

                    // Настраиваем TooltipLabelFormatter для отображения полной информации
                    TooltipLabelFormatter = point =>
                    {
                        var index = (int)point.Model.X;
                        if (index >= 0 && index < labels.Count)
                            return $"{labels[index]}\n{point.Model.Y}x";
                        return "N/A";
                    }
                };

                seriesList.Add(series);
            }

            // Присваиваем массив серий
            SpeedupSeries = seriesList.ToArray();

            // Автоматическое определение пределов оси Y для ускорения
            double minValue = values.Min();
            double maxValue = values.Max();
            double padding = (maxValue - minValue) * 0.1;

            SpeedupXAxes =
            [
                new Axis
                {
                    // Установка Labeler в функцию, возвращающую пустую строку
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
                    MinLimit = Math.Max(0, minValue - padding),
                    MaxLimit = maxValue + padding,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                    Name = "Ускорение (x)",
                    NamePaint = new SolidColorPaint(SKColors.Black)
                }
            ];
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