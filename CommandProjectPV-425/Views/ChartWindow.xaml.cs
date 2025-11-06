using CommandProjectPV_425.Models;
using CommandProjectPV_425.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CommandProjectPV_425.Views
{
    public partial class ChartWindow : Window
    {
        private readonly ChartViewModel _viewModel;

        public ChartWindow()
        {
            InitializeComponent();
            _viewModel = new ChartViewModel();
            DataContext = _viewModel;

            ChartTabControl.SelectionChanged += ChartTabControl_SelectionChanged;
        }

        public void UpdateCharts(IEnumerable<BenchmarkResult> results)
        {
            _viewModel.UpdateCharts(results);
        }

        private async void ChartTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem tabItem)
            {
                if (tabItem.Header.ToString() == "📊 Статистика по методам")
                {
                    await _viewModel.LoadMethodStatisticsAsync();
                }
            }
        }
    }
}