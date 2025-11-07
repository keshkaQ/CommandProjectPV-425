using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;
using CommandProjectPV_425.Services;
using CommandProjectPV_425.ViewModels.Base;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Windows;

namespace CommandProjectPV_425.ViewModels;

public class ChartViewModel : BaseViewModel
{
    private readonly IChartService _chartService;
    private readonly IDataService _dataService;

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
        _dataService = dataService;

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

    public async Task LoadMethodStatisticsAsync()
    {
        try
        {
            var allResults = await _dataService.LoadResultsFromDatabaseAsync();
            // 1. Получение данных из БД
            var stats = _chartService.CalculateAverageTimePerMethod(allResults);

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

        (MethodStatsSeries, MethodStatsXAxes, MethodStatsYAxes) = _chartService.CreateColumnChart(
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
        (TimeSeries, TimeXAxes, TimeYAxes) = _chartService.CreateColumnChart(
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
        (SpeedupSeries, SpeedupXAxes, SpeedupYAxes) = _chartService.CreateColumnChart(
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
}