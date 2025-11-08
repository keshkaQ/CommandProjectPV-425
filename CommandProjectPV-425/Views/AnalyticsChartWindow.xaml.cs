using CommandProjectPV_425.ViewModels;
using System.Windows;

namespace CommandProjectPV_425.Views;

public partial class AnalyticsChartWindow : Window
{
    private readonly AnalyticsChartViewModel _viewModel;
    public AnalyticsChartWindow()
    {
        InitializeComponent();

        _viewModel = new AnalyticsChartViewModel();
        DataContext = _viewModel;

        // Инициализируем асинхронно после загрузки окна
        Loaded += async (s, e) => await _viewModel.InitializeAsync();
    }

    public async Task UpdateChartsAsync()
    {
        await _viewModel.UpdateChartAsync();
    }
}