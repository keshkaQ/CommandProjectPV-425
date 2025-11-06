using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Services;
using CommandProjectPV_425.ViewModels.Base;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;

namespace CommandProjectPV_425.ViewModels;

public class AnalyticsChartViewModel : BaseViewModel
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

    public ISeries[] MethodStatsSeries { get; private set; }

    public Axis[] MethodStatsXAxes { get; private set; }
    public Axis[] MethodStatsYAxes { get; private set; }

    public AnalyticsChartViewModel(IChartService chartService, IDataService dataService)
    {
        _chartService = chartService;
        _dataService=dataService;

        InitializeCharts();
    }

    public AnalyticsChartViewModel() : this(new ChartService(), new DataService())
    {
    }

    private void InitializeCharts()
    {
        MethodStatsSeries = Array.Empty<ISeries>();

        MethodStatsXAxes = Array.Empty<Axis>();
        MethodStatsYAxes = Array.Empty<Axis>();
    }

    public void UpdateChart()
    {
        OnPropertyChanged(nameof(MethodStatsSeries));
        OnPropertyChanged(nameof(MethodStatsXAxes));
        OnPropertyChanged(nameof(MethodStatsYAxes));
    }
}
