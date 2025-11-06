using CommandProjectPV_425.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CommandProjectPV_425.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel(
                new Services.BenchmarkService(),
                new Services.DataService(),
                new Services.ChartService());

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
        private async void ShowChartsBtn_Click(object sender, RoutedEventArgs e) => await _viewModel.OpenCharts();
        private void ClearResultsBtn_Click(object sender, RoutedEventArgs e) => _viewModel.ClearResults();
       
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
