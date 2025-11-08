using CommandProjectPV_425.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CommandProjectPV_425.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly AnalyticsChartViewModel _analyticsChartViewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel(
                new Services.BenchmarkService(),
                new Services.DataService(),
                new Services.ChartService());
            _analyticsChartViewModel = new AnalyticsChartViewModel();

            DataContext = _viewModel;
        }

        private async void RunBenchmarkBtn_Click(object sender, RoutedEventArgs e)
        {
            var (typeTask, size) = GetTypeAndSizeTask();
            if (!string.IsNullOrEmpty(typeTask))
                await _viewModel.RunBenchmarkAsync(typeTask, size);
        }

        private async void SaveToDbBtn_Click(object sender, RoutedEventArgs e) => await _viewModel.SaveToDatabaseAsync();
        private async void ExportBtn_Click(object sender, RoutedEventArgs e) => await _viewModel.LoadFromDatabaseAsync();
        private async void ToJsonBtn_Click(object sender, RoutedEventArgs e) => await _viewModel.SaveToJsonAsync();
        private void ShowChartsBtn_Click(object sender, RoutedEventArgs e) => _viewModel.OpenCharts();
        private async void ExportJsonBtn_Click(object sender, RoutedEventArgs e) => await _viewModel.LoadFromJsonAsync();
        private void ShowAnalyticsBtn_Click(object sender, RoutedEventArgs e) => _viewModel.OpenStatistics();
        private void ClearResultsBtn_Click(object sender, RoutedEventArgs e) => _viewModel.ClearResults();
        // Кнопка для отображения окна справки
        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        // метод для получение размера и типа задачи из combobox
        private (string typeTask, int size) GetTypeAndSizeTask()
        {
            try
            {
                var taskType = ((ComboBoxItem)TaskTypeComboBox.SelectedItem).Content.ToString();
                var sizeText = ((ComboBoxItem)SizeComboBox.SelectedItem).Content.ToString();
                var size = int.Parse(sizeText.Replace(",", ""));
                return (taskType, size);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Выберите тип задачи и размер входных данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return (string.Empty, default);
            }
        }


    }
}