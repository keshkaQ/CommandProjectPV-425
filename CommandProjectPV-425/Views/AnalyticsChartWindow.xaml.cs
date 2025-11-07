using CommandProjectPV_425.Models;
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
    }
    public void UpdateCharts()
    {
        _viewModel.UpdateChartAsync();
    }
}