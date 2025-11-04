using CommandProjectPV_425.Models;
using CommandProjectPV_425.Tests;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Maxim
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
            //ToJsonBtn.IsEnabled = false;
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
            //ToJsonBtn.IsEnabled = true;
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
            //ToJsonBtn.IsEnabled = false;
            ExportBtn.IsEnabled = false;
            RunBenchmarkBtn.IsEnabled = true;
        }

        private (string typeTask, int size) GetTypeAndSizeTask()
        {
            try
            {
                var taskType = ((ComboBoxItem)TaskTypeComboBox.SelectedItem).Content.ToString();
                var sizeText = ((ComboBoxItem)SizeComboBox.SelectedItem).Content.ToString();
                //var countOfRuns = int.Parse(((ComboBoxItem)RunsComboBox.SelectedItem).Content.ToString());
                var size = int.Parse(sizeText.Replace(",", ""));
                return (taskType, size);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Выберите тип задачи, размер входных данных и количество тестов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            //var memoryValues = Results.Select(r => double.Parse(r.MemoryUsed.Replace(" MB", ""))).ToList();
            var speedupValues = Results.Select(r => double.Parse(r.Speedup.Replace("x", ""))).ToList();

            //_chartWindow.UpdateCharts(methodNames, timeValues, memoryValues, speedupValues);
            _chartWindow.UpdateCharts(methodNames, timeValues, speedupValues);
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

                var (typeTask, size) = GetTypeAndSizeTask();
                if (string.IsNullOrEmpty(typeTask)) return;

                var benchmark = InitializeBenchmark(typeTask, size);
                if (benchmark == null) return;

                // Устанавливаем размер массива для всех BenchmarkDotNet тестов
                Environment.SetEnvironmentVariable("BENCHMARK_ARRAY_SIZE", size.ToString());

                // Включаем «крутилку» прогресса
                ProgressBar.IsIndeterminate = true;
                ProgressText.Text = "...";
                StatusText.Text = $"Запуск BenchmarkDotNet для '{typeTask}'...";

                await Task.Delay(150);

                // Конфигурация BenchmarkDotNet 
                var config = ManualConfig
                    .Create(DefaultConfig.Instance)
                    .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                    .AddJob(Job.Dry.WithWarmupCount(1).WithIterationCount(3));

                // Запускаем BenchmarkDotNet
                var summary = BenchmarkRunner.Run(benchmark.GetType(), config);

                // Считываем результаты
                var reports = summary.Reports
                    .Where(r => r.ResultStatistics != null)
                    .Select(r => new
                    {
                        Method = r.BenchmarkCase.Descriptor.WorkloadMethod.Name,
                        // MeanMs = (r.ResultStatistics!.Mean) / 1_000_000.0 // наносекунды → миллисекунды
                        MeanMs = (r.ResultStatistics!.Mean) / 1_000.0 // наносекунды → микросекунды
                    })
                    .ToList();

                if (!reports.Any())
                {
                    MessageBox.Show("BenchmarkDotNet не вернул результаты. Возможно, методы не имеют атрибута [Benchmark].",
                                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusText.Text = "Нет данных о производительности.";
                    TurnOnButtons();
                    return;
                }

                // Определяем базовую линию (Array_For, если есть)
                double baseline = reports.FirstOrDefault(r => r.Method == "Array_For")?.MeanMs
                                  ?? reports.Min(r => r.MeanMs);

                // Создаём экземпляр задачи с тем же размером данных
                var instance = InitializeBenchmark(typeTask, size);
                instance?.GetType().GetMethod("Setup")?.Invoke(instance, null);

                // Локальная функция для получения результата выполнения метода
                int CalcResultFor(string methodName)
                {
                    try
                    {
                        var mi = instance?.GetType().GetMethod(methodName);
                        if (mi == null) return 0;
                        var r = mi.Invoke(instance, null);
                        return r is int i ? i : 0;
                    }
                    catch
                    {
                        return 0;
                    }
                }

                // Очищаем прошлые результаты и заполняем новые
                Results.Clear();
                foreach (var r in reports.OrderBy(x => x.MeanMs))
                {
                    var speedup = (baseline > 0) ? baseline / r.MeanMs : 1.0;
                    Results.Add(new BenchmarkResult
                    {
                        Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Неизвестно",
                        CoreCount = Environment.ProcessorCount,
                        OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                        TaskType = typeTask,
                        DataSize = size,
                        MethodName = r.Method,
                        //ExecutionTime = $"{r.MeanMs:F4} ms",          
                        ExecutionTime = $"{r.MeanMs:F2} µs",
                        Result = CalcResultFor(r.Method).ToString(),   // реальный результат
                        Speedup = $"{speedup:F2}x",
                        Timestamp = DateTime.Now,
                        RawTimes = new List<double> { r.MeanMs }
                    });
                }

                // Обновляем прогресс и статус
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = 100;
                ProgressText.Text = "100%";
                StatusText.Text = $"Тестирование завершено! ({reports.Count} методов)";
                TurnOnButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Ошибка при выполнении тестов";
                TurnOnButtons();
            }
            finally
            {
                ProgressBar.IsIndeterminate = false;
                RunBenchmarkBtn.IsEnabled = true;
            }
        }
        private async void SaveToDbBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveToDbBtn.IsEnabled = false;
            StatusText.Text = "Сохранение результатов в базу данных...";

            try
            {
                using var db = new AppDbContext();

                // Создание базы, если её ещё нет
                await db.Database.EnsureCreatedAsync();

                // Добавление системной информации к каждой записи
                foreach (var result in Results)
                {
                    result.Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Неизвестно";
                    result.CoreCount = Environment.ProcessorCount;
                    result.OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                }

                // Добавление данных и сохранение
                await db.BenchmarkResults.AddRangeAsync(Results);
                await db.SaveChangesAsync();

                StatusText.Text = "Результаты успешно сохранены в базу данных!";
                MessageBox.Show(
                    "Результаты успешно сохранены в базу данных (SQLite)!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при сохранении в БД:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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


        private async void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
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

        //private void SaveToJsonClick(object sender, RoutedEventArgs e)
        //{
        //    SaveResultsToJson();
        //}

        //private async Task SaveResultsToJson()
        //{
        //    try
        //    {
        //        string directoryPath = "BenchmarkResults";
        //        if (!Directory.Exists(directoryPath))
        //        {
        //            Directory.CreateDirectory(directoryPath);
        //        }

        //        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        //        string fileName = $"BenchmarkResults_{timestamp}.json";
        //        string filePath = Path.Combine(directoryPath, fileName);

        //        var resultsToSave = Results.Select(r => new
        //        {
        //            r.TaskType,
        //            r.DataSize,
        //            r.MethodName,
        //            r.ExecutionTime,
        //            r.MemoryUsed,
        //            r.Result,
        //            r.Speedup,
        //            Timestamp = r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
        //            r.Error,
        //            r.StdDev,
        //            r.RawTimes
        //        }).ToList();

        //        var options = new JsonSerializerOptions
        //        {
        //            WriteIndented = true,
        //            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        //        };

        //        string json = JsonSerializer.Serialize(resultsToSave, options);
        //        await File.WriteAllTextAsync(filePath, json);

        //        MessageBox.Show($"Результаты сохранены в файл: {filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Ошибка при сохранении результатов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return;
        //    }
        //}


        //Метод сейчас не использую, все перенесено внутрь SaveToDbBtn_Click 
        /*
        private async Task SaveResultsToDatabase()
        {
            try
            {
                // подключаем только SQLite
                using var db = new AppDbContext();

                // создаем базу и таблицу при первом запуске
                await db.Database.EnsureCreatedAsync();

                // заполняем системные данные
                foreach (var result in Results)
                {
                    result.Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Неизвестно";
                    result.CoreCount = Environment.ProcessorCount;
                    result.OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                }

                // сохраняем результаты
                await db.BenchmarkResults.AddRangeAsync(Results);
                await db.SaveChangesAsync();

                MessageBox.Show("Результаты успешно сохранены в базу данных (SQLite).", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении в SQLite: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        */
    }

}