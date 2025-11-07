using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Windows;

namespace CommandProjectPV_425.ViewModels
{
    public class AnalyticsChartViewModel : BaseViewModel
    {
        private readonly IChartService _chartService;
        private readonly IAnalyticService _analyticsService;

        private ISeries[] _timeStatisticsSeries;
        public ISeries[] TimeStatisticsSeries
        {
            get => _timeStatisticsSeries;
            private set => SetProperty(ref _timeStatisticsSeries, value);
        }

        private Axis[] _timeStatisticsXAxes;
        public Axis[] TimeStatisticsXAxes
        {
            get => _timeStatisticsXAxes;
            private set => SetProperty(ref _timeStatisticsXAxes, value);
        }

        private Axis[] _timeStatisticsYAxes;
        public Axis[] TimeStatisticsYAxes
        {
            get => _timeStatisticsYAxes;
            private set => SetProperty(ref _timeStatisticsYAxes, value);
        }

        public AnalyticsChartViewModel(IChartService chartService, IDataService dataService)
        {
            _chartService = chartService;
            _analyticsService = new AnalyticsService(dataService);

            // Инициализируем пустые коллекции при создании
            ClearCharts();
        }

        public AnalyticsChartViewModel() : this(new ChartService(), new DataService())
        {
        }

        public async Task UpdateChartAsync()
        {
            try
            {
                var stats = await _analyticsService.GetMethodStatisticsAsync();
                UpdateTimeStatisticsChart(stats);
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

        private void UpdateTimeStatisticsChart(List<MethodStatistic> stats)
        {
            if (stats == null || stats.Count == 0)
            {
                ClearCharts();
                return;
            }

            var values = stats.Select(s => s.AverageTimeMs).ToList();
            var labels = stats.Select(s => s.MethodName).ToList();

            var chartData = _chartService.CreateColumnChart(
                labels,
                values,
                "Методы",
                "Среднее время выполнения",
                Helpers.TimeFormatter.FormatTimeShort,
                (index, val) => $"{labels[index]}\n{Helpers.TimeFormatter.FormatTime(val)}"
            );

            TimeStatisticsSeries = chartData.Series;
            TimeStatisticsXAxes = chartData.XAxes;
            TimeStatisticsYAxes = chartData.YAxes;
        }

        private void ClearCharts()
        {
            TimeStatisticsSeries = Array.Empty<ISeries>();
            TimeStatisticsXAxes = Array.Empty<Axis>();
            TimeStatisticsYAxes = Array.Empty<Axis>();
        }
    }
}