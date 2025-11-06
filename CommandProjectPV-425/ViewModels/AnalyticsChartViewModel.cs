using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Services;
using CommandProjectPV_425.ViewModels.Base;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Windows;

namespace CommandProjectPV_425.ViewModels;

public class AnalyticsChartViewModel : BaseViewModel
{
    private readonly IChartService _chartService;
    private readonly IDataService _dataService;

    public ISeries[] TimeStatysticsSeries { get; private set; }

    public Axis[] TimeStatysticsXAxes { get; private set; }
    public Axis[] TimeStatysticsYAxes { get; private set; }

    public AnalyticsChartViewModel(IChartService chartService, IDataService dataService)
    {
        _chartService = chartService;
        _dataService = dataService;
    }

    public AnalyticsChartViewModel() : this(new ChartService(), new DataService())
    {
    }

    public async Task UpdateChartAsync()
    {
        ClearCharts();

        try
        {
            var stats = await _dataService.GetAverageTimePerMethodAsync();
            UpdateTimeStatysticsChart(stats);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ошибка при загрузке статистики методов: {ex.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            ClearCharts();
        }
    }

    public void UpdateTimeStatysticsChart(List<MethodStatistic> stats)
    {
        if (stats == null || stats.Count == 0)
        {
            ClearCharts();
            return;
        }

        var values = stats.Select(s => s.AverageTimeMs).ToList();
        var labels = stats.Select(s => s.MethodName).ToList();

        (TimeStatysticsSeries, TimeStatysticsXAxes, TimeStatysticsYAxes) = _chartService.CreateColumnChart(
            labels,
            values,
            "Методы",
            "Среднее время выполнения",
            Helpers.TimeFormatter.FormatTimeShort,
            (index, val) => $"{labels[index]}\n{Helpers.TimeFormatter.FormatTime(val)}"
        );

        NotifyAllChartPropertiesChanged();
    }

    public void ClearCharts()
    {
        TimeStatysticsSeries = Array.Empty<ISeries>();
        TimeStatysticsXAxes = Array.Empty<Axis>();
        TimeStatysticsYAxes = Array.Empty<Axis>();
        NotifyAllChartPropertiesChanged();
    }

    private void NotifyAllChartPropertiesChanged()
    {
        OnPropertyChanged(nameof(TimeStatysticsSeries));
        OnPropertyChanged(nameof(TimeStatysticsXAxes));
        OnPropertyChanged(nameof(TimeStatysticsYAxes));
    }
}
