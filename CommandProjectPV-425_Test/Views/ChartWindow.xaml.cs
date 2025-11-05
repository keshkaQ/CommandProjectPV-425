using CommandProjectPV_425_Test.Models;
using CommandProjectPV_425_Test.ViewModels;
using System.Windows;

namespace CommandProjectPV_425_Test.Views
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