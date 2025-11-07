using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;
using CommandProjectPV_425.Services;
using CommandProjectPV_425.ViewModels.Base;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using System.Windows;

namespace CommandProjectPV_425.ViewModels;

public class AnalyticsChartViewModel : BaseViewModel
{
    private readonly IChartService _chartService;
    private readonly IDataService _dataService;

    public ISeries[] TimeStatysticsSeries { get; private set; }

    public Axis[] TimeStatysticsXAxes { get; private set; }
    public Axis[] TimeStatysticsYAxes { get; private set; }

    public ObservableCollection<CoreCountOption> CoreCount { get; private set; }

    private CoreCountOption? _selectedCoreCount; // nullable, чтобы можно было выбрать "Все ядра"
    public CoreCountOption? SelectedCoreCount
    {
        get => _selectedCoreCount;
        set
        {
            if (_selectedCoreCount != value)
            {
                _selectedCoreCount = value;
                OnPropertyChanged();
                // Обновляем график при изменении выбора ядра
                _ = UpdateChartAsync(); // Запускаем обновление асинхронно
            }
        }
    }

    public AnalyticsChartViewModel(IChartService chartService, IDataService dataService)
    {
        _chartService = chartService;
        _dataService = dataService;
        CoreCount = new ObservableCollection<CoreCountOption>();
    }

    public AnalyticsChartViewModel() : this(new ChartService(), new DataService())
    {
    }

    public async Task InitializeAsync()
    {
        await LoadCoreCountsAsync();
        // Устанавливаем "null" (все ядра) по умолчанию
        SelectedCoreCount = null;
        await UpdateChartAsync();
    }

    private async Task LoadCoreCountsAsync()
    {
        CoreCount.Clear();

        // Добавляем пункт "Все ядра"
        CoreCount.Add(new CoreCountOption { Value = -1, DisplayName = "Все ядра" });

        var dbCoreCounts = await _dataService.GetCoreCounts();
        foreach (var count in dbCoreCounts)
        {
            CoreCount.Add(new CoreCountOption { Value = count, DisplayName = $"{count}" });
        }
    }

    public async Task UpdateChartAsync()
    {
        ClearCharts();

        try
        {
            // Загружаем все результаты из БД
            var allResults = await _dataService.LoadResultsFromDatabaseAsync();

            // Фильтруем по выбранному количеству ядер, если оно выбрано
            var filteredResults = allResults;
            if (SelectedCoreCount != null && SelectedCoreCount.Value != -1)
            {
                filteredResults = allResults.Where(r => r.CoreCount == SelectedCoreCount.Value).ToList();
            }

            // Вычисляем статистику на основе отфильтрованных результатов
            var stats = _chartService.CalculateAverageTimePerMethod(filteredResults);

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
        OnPropertyChanged(nameof(CoreCount));
        OnPropertyChanged(nameof(SelectedCoreCount));
    }
}

public class CoreCountOption
{
    public int Value { get; set; }
    public string DisplayName { get; set; }

    public override string ToString() => DisplayName; // для отображения в ComboBox
}
