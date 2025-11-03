using CommandProjectPV_425.Models;
using CommandProjectPV_425.Tests;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
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

        private void TurnOffButtons()
        {
            RunBenchmarkBtn.IsEnabled = false;
            SaveToDbBtn.IsEnabled = false;
            ShowChartsBtn.IsEnabled = false;
            ClearResultsBtn.IsEnabled = false;
            ExportBtn.IsEnabled = false;
            ToJsonBtn.IsEnabled = false;
            ProgressBar.Value = 0;
            StatusText.Text = "Подготовка к тестированию...";
            Results.Clear();
        }

        private void TurnOnButtons()
        {
            SaveToDbBtn.IsEnabled = true;
            ShowChartsBtn.IsEnabled = true;
            ClearResultsBtn.IsEnabled = true;
            ExportBtn.IsEnabled = true;
            RunBenchmarkBtn.IsEnabled = true;
            ToJsonBtn.IsEnabled = true;
        }

        private void ClearResultsBtn_Click(object sender, RoutedEventArgs e)
        {
            Results.Clear();
            ProgressBar.Value = 0;
            ProgressText.Text = "0%";
            StatusText.Text = "Готов к тестированию...";
            SaveToDbBtn.IsEnabled = false;
            ShowChartsBtn.IsEnabled = false;
            ClearResultsBtn.IsEnabled = false;
            ToJsonBtn.IsEnabled = false;
            ExportBtn.IsEnabled = false;
            RunBenchmarkBtn.IsEnabled = true;
        }

        private (string typeTask, int size, int numberOfRuns) GetTypeAndSizeTask()
        {
            try
            {
                var taskType = ((ComboBoxItem)TaskTypeComboBox.SelectedItem).Content.ToString();
                var sizeText = ((ComboBoxItem)SizeComboBox.SelectedItem).Content.ToString();
                var countOfRuns = int.Parse(((ComboBoxItem)RunsComboBox.SelectedItem).Content.ToString());
                var size = int.Parse(sizeText.Replace(",", ""));
                return (taskType, size, countOfRuns);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Выберите тип задачи, размер входных данных и количество тестов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return (string.Empty, default,default);
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
                    ("Array PLINQ", countBenchmark.Array_PLINQ),
                    ("Parallel ConcurrentBag", countBenchmark.Parallel_ConcurrentBag),
                    ("Parallel For", countBenchmark.Parallel_For),
                    ("Parallel Partitioner", countBenchmark.Parallel_Partitioner),
                    ("Parallel Invoke", countBenchmark.Parallel_Invoke),
                    ("Tasks Run", countBenchmark.Tasks_Run)
                },


                "Divisible Three or Five" when benchmark is DivisibleThreeOrFive divBenchmark =>
                new List<(string, Func<int>)>
                {
                    ("Array For", divBenchmark.Array_For),
                    ("Array LINQ", divBenchmark.Array_LINQ),
                    ("Array PLINQ", divBenchmark.Array_PLINQ),
                    ("Parallel ConcurrentBag", divBenchmark.Parallel_ConcurrentBag),
                    ("Parallel For", divBenchmark.Parallel_For),
                    ("Parallel Partitioner", divBenchmark.Parallel_Partitioner),
                    ("Parallel Invoke", divBenchmark.Parallel_Invoke),
                    ("Tasks Run", divBenchmark.Tasks_Run)
                },

                "Find Prime Numbers" when benchmark is FindPrimeNumbers primeBenchmark =>
                new List<(string, Func<int>)>
                {
                    ("Array For", primeBenchmark.Array_For),
                    ("Array LINQ", primeBenchmark.Array_LINQ),
                    ("Array PLINQ", primeBenchmark.Array_PLINQ),
                    ("Parallel ConcurrentBag", primeBenchmark.Parallel_ConcurrentBag),
                    ("Parallel For", primeBenchmark.Parallel_For),
                    ("Parallel Partitioner", primeBenchmark.Parallel_Partitioner),
                    ("Parallel Invoke", primeBenchmark.Parallel_Invoke),
                    ("Tasks Run", primeBenchmark.Tasks_Run)
                 },

                "Maximum Of Non Extreme Elements" when benchmark is MaximumOfNonExtremeElements extremeBenchmark =>
                new List<(string, Func<int>)>
                {
                     ("Array For", extremeBenchmark.Array_For),
                    ("Array LINQ", extremeBenchmark.Array_LINQ),
                    ("Array PLINQ", extremeBenchmark.Array_PLINQ),
                    ("Parallel ConcurrentBag", extremeBenchmark.Parallel_ConcurrentBag),
                    ("Parallel For", extremeBenchmark.Parallel_For),
                    ("Parallel Partitioner", extremeBenchmark.Parallel_Partitioner),
                    ("Parallel Invoke", extremeBenchmark.Parallel_Invoke),
                    ("Tasks Run", extremeBenchmark.Tasks_Run)
                },
                _ => new List<(string, Func<int>)>()
            };
        }

        private async void RunBenchmarkBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TurnOffButtons(); 
                var (typeTask, size, numberOfRuns) = GetTypeAndSizeTask();
                if (string.IsNullOrEmpty(typeTask)) return;

                var benchmark = InitializeBenchmark(typeTask, size);
                if (benchmark == null)  return;

                var setupMethod = benchmark.GetType().GetMethod("Setup");
                setupMethod?.Invoke(benchmark, null);

                var tests = GetTestsForTask(typeTask, benchmark);
                if (tests == null || tests.Count == 0)
                {
                    MessageBox.Show("Не найдены тесты для выбранной задачи", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                double baselineTime = 0;

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

                    UpdateProgress((i + 1) * 100 / tests.Count);
                }

                StatusText.Text = $"Тестирование завершено! Протестировано {tests.Count} методов для задачи '{typeTask}'.";
                TurnOnButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Ошибка при выполнении тестов";
                TurnOnButtons(); 
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

        private void UpdateProgress(double value, string text = null)
        {
            ProgressBar.Value = value;
            ProgressText.Text = $"{value:F0}%";
            if (!string.IsNullOrEmpty(text))
                StatusText.Text = text;
        }


        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveToJsonClick(object sender, RoutedEventArgs e)
        {
            SaveResultsToJson();
        }

        private async Task SaveResultsToJson()
        {
            try
            {
                string directoryPath = "BenchmarkResults";
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileName = $"BenchmarkResults_{timestamp}.json";
                string filePath = Path.Combine(directoryPath, fileName);

                var resultsToSave = Results.Select(r => new
                {
                    r.TaskType,
                    r.DataSize,
                    r.MethodName,
                    r.ExecutionTime,
                    r.MemoryUsed,
                    r.Result,
                    r.Speedup,
                    Timestamp = r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    r.Error,
                    r.StdDev,
                    r.RawTimes
                }).ToList();

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(resultsToSave, options);
                await File.WriteAllTextAsync(filePath, json);

                MessageBox.Show($"Результаты сохранены в файл: {filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении результатов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private async Task SaveResultsToDatabase()
        {
            // реализовать
            try
            {
                MessageBox.Show("Результаты сохранены в базе данных");
            }
            catch(Exception ex)
            {

            }

        }
    }
}