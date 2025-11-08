using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;
using CommandProjectPV_425.Views;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CommandProjectPV_425.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IBenchmarkService _benchmarkService;  // запуск тестов производительности
        private readonly IDataService _dataService;            // работа с данными (БД, JSON)
        private readonly IChartService _chartService;          // подготовка данных для графиков

        public ObservableCollection<BenchmarkResult> Results { get; set; } // Коллекция результатов для datagrid

        // Блок ошибок
        private Visibility _errorVisibility = Visibility.Collapsed;
        public Visibility ErrorVisibility
        {
            get => _errorVisibility;
            set { _errorVisibility = value; OnPropertyChanged(); }
        }

        private string _errorText;
        public string ErrorText
        {
            get => _errorText;
            set { _errorText = value; OnPropertyChanged(); }
        }

        // Блок статуса
        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        // флаг для блокировки кнопок
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }

        public bool IsNotBusy => !IsBusy;

        public MainViewModel(IBenchmarkService benchmarkService, IDataService dataService, IChartService chartService)
        {
            _benchmarkService = benchmarkService;
            _dataService = dataService;
            _chartService = chartService;
            Results = new ObservableCollection<BenchmarkResult>();
            StatusText = "Готов к тестированию...";
            IsBusy = false;
        }

        // Универсальный метод для отображения ошибок
        private void ShowError(string errorMessage)
        {
            ErrorText = errorMessage;
            ErrorVisibility = Visibility.Visible;
        }

        // Метод для скрытия ошибок
        private void HideError()
        {
            ErrorVisibility = Visibility.Collapsed;
            ErrorText = string.Empty;
        }

        // Метод для отображения успешного статуса
        private void ShowSuccess(string message)
        {
            StatusText = message;
            HideError();
        }
        public async Task RunBenchmarkAsync(string taskType, int size)
        {
            if (IsBusy) return;
            var res = MessageBox.Show("Продолжительность тестов занимает больше 5 минут. Вы готовы подождать?", "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.No) return;

            try
            {
                IsBusy = true;
                HideError();
                StatusText = "Запуск тестов. Немного подождите...";

                // Добавление списка всех задач
                var allTasks = new[]
                {
                    "Count Numbers Above Average",
                    "Divisible Three or Five",
                    "Find Prime Numbers",
                    "Maximum Of Non Extreme Elements",
                    "Max Frequency Of Elements"
                };

                // Проверка выбран ли пункт все задачи
                bool runAllTasks = taskType == "All Tasks";

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Results.Clear();
                });

                if (runAllTasks)
                {
                    int total = allTasks.Length;
                    int completed = 0;

                    foreach (var currentTask in allTasks)
                    {
                        StatusText = $"Выполняется: {currentTask}... ({completed + 1}/{total})";

                        var results = await _benchmarkService.RunBenchmarkAsync(currentTask, size);

                        // Добавляем результаты каждой задачи по мере выполнения
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var result in results)
                                Results.Add(result);
                        });

                        completed++;
                    }

                    // Сортировка всех результатов после завершения всех задач
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var sortedResults = Results.OrderBy(r => ParseExecutionTime(r.ExecutionTime)).ToList();
                        Results.Clear();
                        foreach (var result in sortedResults)
                            Results.Add(result);
                    });

                    ShowSuccess("Тестирование завершено! Все задачи успешно выполнены.");
                }
                else
                {
                    var results = await _benchmarkService.RunBenchmarkAsync(taskType, size);

                    // Сортировка результатов для одиночной задачи
                    var sortedResults = results.OrderBy(r => ParseExecutionTime(r.ExecutionTime)).ToList();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Results.Clear();
                        foreach (var result in sortedResults)
                            Results.Add(result);
                    });

                    ShowSuccess($"Тестирование завершено! Протестировано {sortedResults.Count} методов.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при выполнении тестов: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Вспомогательный метод для парсинга времени выполнения
        private double ParseExecutionTime(string executionTime)
        {
            var timeStr = executionTime.Replace(" ms", "").Replace(",", ".");
            if (double.TryParse(timeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double timeMs))
                return timeMs;
            return double.MaxValue;
        }

        public async Task SaveToDatabaseAsync()
        {
            if (IsBusy || !Results.Any())
            {
                ShowError("Нет результатов для сохранения");
                return;
            }

            try
            {
                IsBusy = true;
                HideError();
                StatusText = "Сохранение результатов в базу данных...";

                await _dataService.SaveResultsToDatabaseAsync(Results);
                ShowSuccess("Результаты успешно сохранены в базу данных!");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при сохранении в БД: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadFromDatabaseAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                HideError();
                StatusText = "Загрузка данных из базы...";

                var results = await _dataService.LoadResultsFromDatabaseAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Results.Clear();
                    foreach (var item in results)
                        Results.Add(item);
                });

                if (results.Any())
                    ShowSuccess($"Загружено {results.Count} записей из БД.");
                else
                    ShowSuccess("База данных пуста.");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке данных из базы: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void OpenCharts()
        {
            if (!Results.Any())
            {
                ShowError("Нет данных для отображения графиков");
                return;
            }

            var chartWindow = new ChartWindow();
            chartWindow.UpdateCharts(Results);
            chartWindow.Show();
        }
        public void OpenStatistics()
        {
            if (!Results.Any())
            {
                ShowError("Для построения графиков, загрузите данные с базы данных");
                return;
            }
            var analyticWindow = new AnalyticsChartWindow();
            analyticWindow.Show();
        }

        public async Task SaveToJsonAsync()
        {
            if (IsBusy || !Results.Any())
            {
                ShowError("Нет результатов для сохранения в JSON");
                return;
            }

            try
            {
                IsBusy = true;
                HideError();
                StatusText = "Сохранение в JSON...";

                await _dataService.SaveResultsToJsonAsync(Results);
                ShowSuccess("Результаты сохранены в JSON файл");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при сохранении в JSON: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadFromJsonAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FilterIndex = 1,
                Title = "Выберите JSON файл с результатами тестов",
                DefaultExt = "json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                if (!selectedFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    ShowError("Выбранный файл не является JSON файлом");
                    return;
                }

                try
                {
                    IsBusy = true;
                    HideError();
                    StatusText = "Загрузка данных из JSON...";

                    var json = await File.ReadAllTextAsync(selectedFilePath);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        ShowError("JSON файл пуст");
                        return;
                    }

                    var results = await _dataService.LoadResultsFromJsonAsync(json);

                    if (results == null || !results.Any())
                    {
                        ShowError("В JSON файле нет данных");
                        return;
                    }

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Results.Clear();
                        foreach (var result in results)
                            Results.Add(result);
                    });

                    ShowSuccess($"Загружено {results.Count} записей из JSON файла");
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка загрузки данных из JSON: {ex.Message}");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        public void ClearResults()
        {
            if (IsBusy) return;

            Results.Clear();
            HideError();
            StatusText = "Готов к тестированию...";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}