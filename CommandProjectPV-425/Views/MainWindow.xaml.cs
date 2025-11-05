using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using CommandProjectPV_425.Models;
using CommandProjectPV_425.Tests;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System;
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
            ExportBtn.IsEnabled = false;
            ClearResultsBtn.IsEnabled = false;
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
                MessageBox.Show("Нет данных для отображения графиков.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var chartWindow = new ChartWindow();

            var successfulResults = Results
                .Where(r => r.ExecutionTime != "Failed")
                .OrderBy(r => r.TaskType) // Сортируем по типу задачи
                .ThenBy(r => r.MethodName) // Затем по методу
                .ToList();

            if (!successfulResults.Any())
            {
                MessageBox.Show("Нет успешных результатов.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var labels = new List<string>();
            var timeValues = new List<double>();
            var speedupValues = new List<double>();

            foreach (var result in successfulResults)
            {
                // Сокращаем подписи
                var taskAbbr = result.TaskType.Length > 15 ?
                    result.TaskType.Substring(0, 15) + "..." : result.TaskType;
                var methodAbbr = result.MethodName.Length > 10 ?
                    result.MethodName.Substring(0, 10) + "..." : result.MethodName;

                labels.Add($"{taskAbbr}\n{methodAbbr}");
                timeValues.Add(ParseTimeToMs(result.ExecutionTime));
                speedupValues.Add(ParseSpeedup(result.Speedup));
            }

            chartWindow.UpdateCharts(labels, timeValues, speedupValues);
            chartWindow.Show();
        }
        private double ParseTimeToMs(string timeStr)
        {
            try
            {
                if (timeStr.EndsWith(" s"))
                    return double.Parse(timeStr.Replace(" s", "")) * 1000;
                else if (timeStr.EndsWith(" ms"))
                    return double.Parse(timeStr.Replace(" ms", ""));
                else if (timeStr.EndsWith(" μs"))
                    return double.Parse(timeStr.Replace(" μs", "")) / 1000;
                else if (timeStr.EndsWith(" ns"))
                    return double.Parse(timeStr.Replace(" ns", "")) / 1_000_000;
                else
                    return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        private double ParseSpeedup(string speedupStr)
        {
            try
            {
                return double.Parse(speedupStr.Replace("x", "").Trim());
            }
            catch
            {
                return 0.0;
            }
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

                // Конфигурация BenchmarkDotNet 
                var config = ManualConfig
                    .Create(DefaultConfig.Instance)
                    .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                    .AddJob(Job.Dry.WithWarmupCount(1).WithIterationCount(1));

                // Запускаем бенчмарк в отдельном потоке чтобы не блокировать UI
                await Task.Run(() =>
                {
                    var summary = BenchmarkRunner.Run(benchmarkType, config);
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
                    Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Неизвестно",
                    CoreCount = Environment.ProcessorCount,
                    OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
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
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 100;
            ProgressText.Text = "100%";
        }


        private async void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            TurnOnButtons();
            try
            {
                using var db = new AppDbContext();
                StatusText.Text = "Загрузка данных из базы...";
                ProgressBar.IsIndeterminate = true;

                var resultsFromDb = await db.BenchmarkResults
                    .OrderByDescending(r => r.Timestamp)
                    .ToListAsync();

                if (resultsFromDb.Count == 0)
                {
                    MessageBox.Show("В базе данных пока нет сохранённых результатов.",
                                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusText.Text = "База данных пуста.";
                }
                else
                {
                    Results.Clear();
                    foreach (var item in resultsFromDb)
                        Results.Add(item);

                    MessageBox.Show($"Загружено {resultsFromDb.Count} записей из базы данных.",
                                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusText.Text = $"Загружено {resultsFromDb.Count} записей из БД.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте из базы данных: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Ошибка при экспорте из БД.";
            }
            finally
            {
                ProgressBar.IsIndeterminate = false;
            }
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
            try
            {
                using var context = new AppDbContext();

                // Создаем базу данных и таблицы, если их нет
                await context.Database.EnsureCreatedAsync();

                // Добавляем все результаты
                await context.BenchmarkResults.AddRangeAsync(Results);
                await context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
