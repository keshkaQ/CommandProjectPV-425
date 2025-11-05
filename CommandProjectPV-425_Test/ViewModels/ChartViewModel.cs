using CommandProjectPV_425_Test.Interfaces;
using CommandProjectPV_425_Test.Models;
using CommandProjectPV_425_Test.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CommandProjectPV_425_Test.ViewModels
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

        private void UpdateTimeChart(List<string> labels, List<double> values)
        {
            var points = new List<ObservablePoint>();
            for (int i = 0; i < values.Count; i++)
            {
                points.Add(new ObservablePoint(i, values[i]));
            }

            var series = new LineSeries<ObservablePoint>
            {
                Values = points,
                Name = "Время выполнения",
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
                        return $"{labels[index]}\n{Helpers.TimeFormatter.FormatTime(point.Model.Y)}";
                    return "N/A";
                }
            };

            TimeSeries = [series];

            // Автоматическое определение пределов оси Y
            double minValue = values.Min();
            double maxValue = values.Max();
            double padding = (maxValue - minValue) * 0.1;

            TimeXAxes =
            [
                new Axis
                {
                    Labels = labels.ToArray(),
                    LabelsRotation = 45,
                    TextSize = 10,
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                    ForceStepToMin = true,
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
            var points = new List<ObservablePoint>();
            for (int i = 0; i < values.Count; i++)
            {
                points.Add(new ObservablePoint(i, values[i]));
            }

            var series = new LineSeries<ObservablePoint>
            {
                Values = points,
                Name = "Ускорение",
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

            SpeedupSeries = [series];

            // Автоматическое определение пределов оси Y для ускорения
            double minValue = values.Min();
            double maxValue = values.Max();
            double padding = (maxValue - minValue) * 0.1;

            SpeedupXAxes =
            [
                new Axis
                {
                    Labels = labels.ToArray(),
                    LabelsRotation = 45,
                    TextSize = 10,
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                    ForceStepToMin = true,
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