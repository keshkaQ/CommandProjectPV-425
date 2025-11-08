using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;
using CommandProjectPV_425.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace CommandProjectPV_425.ViewModels;

public class ChartViewModel : BaseViewModel
{
    private readonly IChartService _chartService;
    private readonly IDataService _dataService;

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
            "Ускорение (%)",
              v => $"{v:0.##}%", // Форматируем как проценты
            (index, val) => $"{labels[index]}\n{FormatSpeedupTooltip(val)}",
            true
        );
    }

    private string FormatSpeedupTooltip(double value)
    {
        if (value > 100)
        {
            return $"+{value:0.##}% (ускорение на {(value - 100):0.##}%)";
        }
        else if (value < 100)
        {
            return $"{value:0.##}% (замедление на {(100 - value):0.##}%)";
        }
        else
        {
            return "100% (без изменений)";
        }
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
    }
}