using CommandProjectPV_425.Tests;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace CommandProjectPV_425.Views
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<BenchmarkResult> Results { get; set; }
        private ChartWindow _chartWindow;

        public MainWindow()
        {
            InitializeComponent();

            Results = new ObservableCollection<BenchmarkResult>();
            ResultsDataGrid.ItemsSource = Results;
            DataContext = this;
        }

        private void ResetApp()
        {
            RunBenchmarkBtn.IsEnabled = false;
            SaveToDbBtn.IsEnabled = false;
            ShowChartsBtn.IsEnabled = false;
            ProgressBar.Value = 0;
            StatusText.Text = "Подготовка к тестированию...";
            Results.Clear();
        }

        private void ClearResultsBtn_Click(object sender, RoutedEventArgs e)
        {
            Results.Clear();
            ProgressBar.Value = 0;
            StatusText.Text = "Готов к тестированию...";
            SaveToDbBtn.IsEnabled = false;
            ShowChartsBtn.IsEnabled = false;
        }

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
                MessageBox.Show("Не удалось получить тип задачи или размер", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return (string.Empty, default);
            }
        }

        private object InitializeBenchmark(string taskType, int size)
        {
            try
            {
                return taskType switch
                {
                    "Count Numbers Above Average" => new CountAboveAverage(size),
                    "Divisible Three or Five" => new DivisibleThreeOrFive(size),
                    "Find Prime Numbers" => new FindPrimeNumbers(size),
                    "Maximum Of Non Extreme Elements" => new MaximumOfNonExtremeElements(size),
                    _ => throw new ArgumentException($"Неизвестный тип задачи: {taskType}")
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации теста: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void ShowChartsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Results.Any())
            {
                MessageBox.Show("Нет данных для отображения графиков. Сначала запустите тестирование.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_chartWindow == null || !_chartWindow.IsLoaded)
            {
                _chartWindow = new ChartWindow();
                _chartWindow.Closed += (s, args) => _chartWindow = null;
            }

            // Обновляем графики данными
            var methodNames = Results.Select(r => r.MethodName).ToList();
            var timeValues = Results.Select(r => double.Parse(r.ExecutionTime.Replace(" ms", ""))).ToList();
            var memoryValues = Results.Select(r => double.Parse(r.MemoryUsed.Replace(" MB", ""))).ToList();
            var speedupValues = Results.Select(r => double.Parse(r.Speedup.Replace("x", ""))).ToList();

            _chartWindow.UpdateCharts(methodNames, timeValues, memoryValues, speedupValues);
            _chartWindow.Show();
            _chartWindow.Focus();
        }

        private List<(string Name, Func<int> Test)> GetTestsForTask(string taskType, object benchmark)
        {
            return taskType switch
            {
                "Count Numbers Above Average" when benchmark is CountAboveAverage countBenchmark =>
                new List<(string, Func<int>)>
                {
                    ("Array For", countBenchmark.Array_For),
                     
                    ("Array LINQ", countBenchmark.Array_LINQ),
                    ("List LINQ", countBenchmark.List_LINQ),
                    ("List Foreach", countBenchmark.List_Foreach),
                    ("Parallel For Local", countBenchmark.Parallel_For_Local),
                    ("Parallel ForEach Partitioner", countBenchmark.Parallel_ForEach_Partitioner),
                    ("Parallel ConcurrentBag", countBenchmark.Parallel_ConcurrentBag),
                    ("PLINQ Auto", countBenchmark.PLINQ_AutoParallel),
                    ("PLINQ With Degree", countBenchmark.PLINQ_WithDegreeOfParallelism),
                    ("PLINQ Force Parallel", countBenchmark.PLINQ_ForceParallel),
                    ("Tasks Run", countBenchmark.Tasks_Run),
                    ("Tasks Factory", countBenchmark.Tasks_Factory)
                },


                "Divisible Three or Five" when benchmark is DivisibleThreeOrFive divBenchmark =>
                new List<(string, Func<int>)>
                {
                    ("Array For", divBenchmark.Array_For),
                    ("List For", divBenchmark.List_For),
                    ("Array LINQ", divBenchmark.Array_LINQ),
                    ("Tasks By Cores", divBenchmark.List_LINQ),
                    ("Parallel ForEach Partitioner", divBenchmark.Parallel_ForEach_Partitioner),
                    ("Parallel For Local", divBenchmark.Parallel_For_Local),
                    ("Parallel Concurrent Bag", divBenchmark.Parallel_ConcurrentBag),
                    ("PLINQ Auto Parallel", divBenchmark.PLINQ_AutoParallel),
                    ("PLINQ With Degree Of Parallelism", divBenchmark.PLINQ_WithDegreeOfParallelism),
                    ("PLINQ Force Parallel", divBenchmark.PLINQ_ForceParallel),
                    ("Tasks Run", divBenchmark.Tasks_Run),
                    ("Tasks Factory", divBenchmark.Tasks_Factory),
                },

                "Find Prime Numbers" when benchmark is FindPrimeNumbers primeBenchmark =>
                new List<(string, Func<int>)>
                {
                    ("Array For", primeBenchmark.Array_For),
                    ("Parallel For", primeBenchmark.Parallel_For),
                    ("PLINQ Array", primeBenchmark.Array_PLINQ),
                    ("Tasks By Cores", primeBenchmark.Tasks_By_Cores),
                    ("Parallel ForEach", primeBenchmark.Parallel_ForEach),
                    ("Parallel Invoke", primeBenchmark.Parallel_Invoke),
                    ("Parallel ForEach ConcurrentBag", primeBenchmark.Parallel_ForEach_ConcurrentBag),
                    ("Parallel For Lists", primeBenchmark.Parallel_For_Lists),
                    ("Tasks Run", primeBenchmark.Tasks_Run),
                    ("PLINQ With Degree", primeBenchmark.PLINQ_WithDegreeOfParallelism),
                    ("Partitioner ForEach", primeBenchmark.Partitioner_ForEach)
                 },

                "Maximum Of Non Extreme Elements" when benchmark is MaximumOfNonExtremeElements extremeBenchmark =>
                new List<(string, Func<int>)>
                {
                    ("Array For", extremeBenchmark.Array_For),
                    ("List For", extremeBenchmark.List_For),
                    ("Parallel For ConcurrentBag", extremeBenchmark.Parallel_For_ConcurrentBag),
                    ("Parallel For", extremeBenchmark.Parallel_For),
                    ("Parallel Foreach Partitioner", extremeBenchmark.Parallel_Foreach_Partitioner),
                    ("List LINQ", extremeBenchmark.List_LINQ),
                    ("Array LINQ", extremeBenchmark.Array_LINQ),
                    ("List PLINQ", extremeBenchmark.List_PLINQ),
                    ("Array PLINQ", extremeBenchmark.Array_PLINQ),
                    ("Tasks Run", extremeBenchmark.Tasks_Run)
                },
                _ => new List<(string, Func<int>)>()
            };
        }

        private async void RunBenchmarkBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResetApp();
                var (typeTask, size) = GetTypeAndSizeTask();
                if (string.IsNullOrEmpty(typeTask)) return;

                var benchmark = InitializeBenchmark(typeTask, size);
                if (benchmark == null) return;

                var setupMethod = benchmark.GetType().GetMethod("Setup");
                setupMethod?.Invoke(benchmark, null);

                var tests = GetTestsForTask(typeTask, benchmark);
                if (tests == null || tests.Count == 0)
                {
                    MessageBox.Show("Не найдены тесты для выбранной задачи", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                double baselineTime = 0;
                const int numberOfRuns = 10;

                for (int i = 0; i < tests.Count; i++)
                {
                    var test = tests[i];
                    StatusText.Text = $"Выполнение: {test.Name}... ({i + 1}/{tests.Count})";

                    var timeMeasurements = new List<double>();
                    var memoryMeasurements = new List<double>();
                    object firstResult = null;

                    for (int run = 0; run < numberOfRuns; run++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        var memoryBefore = GC.GetTotalMemory(true);
                        var stopwatch = Stopwatch.StartNew();
                        var result = await Task.Run(() => test.Test());
                        stopwatch.Stop();
                        var memoryAfter = GC.GetTotalMemory(false);

                        if (run == 0) firstResult = result;

                        timeMeasurements.Add(stopwatch.Elapsed.TotalMilliseconds);
                        memoryMeasurements.Add(Math.Max(0, (memoryAfter - memoryBefore) / (1024 * 1024.0)));

                        await Task.Delay(50);
                    }
                    var timeStats = BenchmarkResult.CalculateStatistics(timeMeasurements);
                    var memoryStats = BenchmarkResult.CalculateStatistics(memoryMeasurements);

                    if (i == 0) baselineTime = timeStats.mean;

                    var speedup = baselineTime > 0 ? baselineTime / timeStats.mean : 1;

                    Results.Add(new BenchmarkResult
                    {
                        TaskType = typeTask,
                        DataSize = size,
                        MethodName = test.Name,
                        ExecutionTime = timeStats.mean.ToString("F2") + " ms",
                        MemoryUsed = memoryStats.mean.ToString("F2") + " MB",
                        Result = firstResult?.ToString() ?? "0",
                        Speedup = speedup.ToString("F2") + "x",
                        Timestamp = DateTime.Now,
                        Error = timeStats.error.ToString("F4") + " ms",
                        StdDev = timeStats.stdDev.ToString("F4") + " ms",
                        RawTimes = timeMeasurements
                    });

                    ProgressBar.Value = (i + 1) * 100 / tests.Count;
                }

                StatusText.Text = $"Тестирование завершено! Протестировано {tests.Count} методов для задачи '{typeTask}'.";
                SaveToDbBtn.IsEnabled = true;
                ShowChartsBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Ошибка при выполнении тестов";
            }
            finally
            {
                RunBenchmarkBtn.IsEnabled = true;
            }
        }
        private async void SaveToDbBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveToDbBtn.IsEnabled = false;
                StatusText.Text = "Сохранение результатов в базу данных...";

                await SaveResultsToDatabase();

                StatusText.Text = "Результаты успешно сохранены в базу данных!";
                MessageBox.Show("Результаты успешно сохранены в базу данных!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении в БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Ошибка при сохранении в БД";
            }
            finally
            {
                SaveToDbBtn.IsEnabled = true;
            }
        }

        private async Task SaveResultsToDatabase()
        {
            // Реализовать 
            await Task.Delay(500);
            MessageBox.Show("Результаты сохранены в базе данных");
        }
    }
}