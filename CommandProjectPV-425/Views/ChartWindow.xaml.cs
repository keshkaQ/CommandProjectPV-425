using CommandProjectPV_425.Models;
using CommandProjectPV_425.ViewModels;
using System.Windows;

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
        }

        public void UpdateCharts(IEnumerable<BenchmarkResult> results)
        {
            _viewModel.UpdateCharts(results);
        }
    }
}