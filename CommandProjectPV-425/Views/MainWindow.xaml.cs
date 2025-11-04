using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using CommandProjectPV_425.Models;
using CommandProjectPV_425.Tests;
using System.Collections.ObjectModel;
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

        private Type GetBenchmarkType(string taskType)
        {
            return taskType switch
            {
                "Count Numbers Above Average" => typeof(CountAboveAverage),
                "Divisible Three or Five" => typeof(DivisibleThreeOrFive),
                "Find Prime Numbers" => typeof(FindPrimeNumbers),
                "Maximum Of Non Extreme Elements" => typeof(MaximumOfNonExtremeElements),
                "Max Frequency Of Elements" => typeof(MaxFrequencyOfElements),
                _ => throw new ArgumentException($"Неизвестный тип задачи: {taskType}")
            };
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

            // Фильтруем только успешные результаты
            var successfulResults = Results.Where(r => r.ExecutionTime != "Failed").ToList();

            if (!successfulResults.Any())
            {
                MessageBox.Show("Нет успешных результатов для отображения графиков.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var methodNames = successfulResults.Select(r => r.MethodName).ToList();

            // Парсим время выполнения
            var timeValues = successfulResults.Select(r =>
            {
                var timeStr = r.ExecutionTime;
                if (timeStr.EndsWith(" s"))
                    return double.Parse(timeStr.Replace(" s", "")) * 1000; // Конвертируем в мс
                else if (timeStr.EndsWith(" ms"))
                    return double.Parse(timeStr.Replace(" ms", ""));
                else if (timeStr.EndsWith(" μs"))
                    return double.Parse(timeStr.Replace(" μs", "")) / 1000; // Конвертируем в мс
                else
                    return 0.0;
            }).ToList();

            // Парсим ускорение
            var speedupValues = successfulResults.Select(r =>
            {
                var speedupStr = r.Speedup;
                return double.Parse(speedupStr.Replace("x", ""));
            }).ToList();

            // Пустой список для памяти (больше не используется)
            var emptyMemoryValues = new List<double>();

            _chartWindow.UpdateCharts(methodNames, timeValues, emptyMemoryValues, speedupValues);
            _chartWindow.Show();
            _chartWindow.Focus();
        }

        private async void RunBenchmarkBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TurnOffButtons();
                var (typeTask, size) = GetTypeAndSizeTask();
                if (string.IsNullOrEmpty(typeTask)) return;

                var benchmarkType = GetBenchmarkType(typeTask);
                if (benchmarkType == null)
                {
                    MessageBox.Show("Не удалось определить тип бенчмарка", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusText.Text = "Запуск BenchmarkDotNet...";

                // Устанавливаем размер данных для бенчмарка
                SetBenchmarkSize(benchmarkType, size);

                // Запускаем бенчмарк в отдельном потоке чтобы не блокировать UI
                await Task.Run(() =>
                {
                    var summary = BenchmarkRunner.Run(benchmarkType);
                    ProcessBenchmarkSummary(summary, typeTask, size);
                });

                StatusText.Text = $"Тестирование завершено! Протестировано {Results.Count} методов для задачи '{typeTask}'.";
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

        private void SetBenchmarkSize(Type benchmarkType, int size)
        {
            // Устанавливаем размер данных через статические свойства
            if (benchmarkType == typeof(CountAboveAverage))
                CountAboveAverage.Size = size;
            else if (benchmarkType == typeof(DivisibleThreeOrFive))
                DivisibleThreeOrFive.Size = size;
            else if (benchmarkType == typeof(FindPrimeNumbers))
                FindPrimeNumbers.Size = size;
            else if (benchmarkType == typeof(MaximumOfNonExtremeElements))
                MaximumOfNonExtremeElements.Size = size;
            else if (benchmarkType == typeof(MaxFrequencyOfElements))
                MaxFrequencyOfElements.Size = size;
        }

        private void ProcessBenchmarkSummary(Summary summary, string taskType, int dataSize)
        {
            Dispatcher.Invoke(() => Results.Clear());

            // Найти базовый метод (тот, который помечен [Benchmark(Baseline = true)])
            var baselineReport = summary.Reports.FirstOrDefault(r =>
                r.BenchmarkCase.Descriptor.Baseline);

            double baselineTimeNs = 0;
            bool baselineFound = false;

            if (baselineReport != null && baselineReport.Success && baselineReport.ResultStatistics != null)
            {
                baselineTimeNs = baselineReport.ResultStatistics.Mean;
                baselineFound = true;
            }

            int totalReports = summary.Reports.Count();
            int currentReport = 0;

            foreach (var report in summary.Reports)
            {
                currentReport++;
                var benchmarkCase = report.BenchmarkCase;
                var methodName = FormatMethodName(benchmarkCase.Descriptor.WorkloadMethod.Name);

                var result = report.ResultStatistics;
                var meanTimeNs = result.Mean;
                var meanTimeUs = meanTimeNs / 1_000.0;

                // Расчет ускорения относительно базового метода
                var speedup = baselineTimeNs / meanTimeNs;

                // Форматируем ускорение
                string speedupFormatted = speedup switch
                {
                    < 0.01 => speedup.ToString("F3") + "x",
                    < 0.1 => speedup.ToString("F3") + "x",
                    < 1 => speedup.ToString("F2") + "x",
                    _ => speedup.ToString("F2") + "x"
                };

                var benchmarkResult = new BenchmarkResult
                {
                    TaskType = taskType,
                    DataSize = dataSize,
                    MethodName = methodName,
                    ExecutionTime = FormatTimeUs(meanTimeUs),
                    Speedup = speedupFormatted,
                    Timestamp = DateTime.Now,
                    RawTimes = new List<double> { meanTimeUs }
                };

                Dispatcher.Invoke(() => Results.Add(benchmarkResult));

                var progress = (currentReport * 100) / totalReports;
                Dispatcher.Invoke(() => UpdateProgress(progress, $"Обработка: {methodName}"));
            }
        }

        private string FormatTimeUs(double timeUs)
        {
            if (timeUs >= 1000)
                return (timeUs / 1000).ToString("F2") + " ms";
            else if (timeUs >= 1)
                return timeUs.ToString("F1") + " μs";
            else
                return (timeUs * 1000).ToString("F1") + " ns";
        }


        private string FormatMethodName(string methodName)
        {
            return methodName switch
            {
                "Array_For" => "Array For",
                "Array_PLINQ" => "Array PLINQ",
                "Parallel_For" => "Parallel For",
                "Parallel_Partitioner" => "Parallel Partitioner",
                "Parallel_Invoke" => "Parallel Invoke",
                "Tasks_Run" => "Tasks Run",
                "Array_SIMD" => "Array SIMD",
                "Array_SIMD_Intrinsics" => "Array SIMD  Intrinsics",
                "Array_Unsafe" => "Array Unsafe",
                _ => methodName
            };
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
            // Реализация
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
                    r.Speedup,
                    Timestamp = r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
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
            catch (Exception ex)
            {
                // Обработка ошибок
            }
        }
    }
}