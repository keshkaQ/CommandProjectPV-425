using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;
using CommandProjectPV_425.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CommandProjectPV_425.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IBenchmarkService _benchmarkService;  // запуск тестов производительности
        private readonly IDataService _dataService;            // работа с данными (БД, JSON)
        private readonly IChartService _chartService;          // подготовка данных для графиков

        public ObservableCollection<BenchmarkResult> Results { get; set; } // Коллекция результатов для datagrid

        // строка статуса в нижней части окна
        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        // значение прогресс-бара 
        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
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

        public async Task RunBenchmarkAsync(string taskType, int size)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                StatusText = "Запуск тестов. Немного подождите...";
                ProgressValue = 0;

                var results = await _benchmarkService.RunBenchmarkAsync(taskType, size);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Results.Clear();
                    foreach (var result in results)
                        Results.Add(result);
                });

                StatusText = $"Тестирование завершено! Протестировано {results.Count} методов.";
                ProgressValue = 100;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "Ошибка при выполнении тестов";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SaveToDatabaseAsync()
        {
            if (IsBusy || !Results.Any())
            {
                MessageBox.Show("Нет результатов для сохранения","Информация",MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsBusy = true;
                StatusText = "Сохранение результатов в базу данных...";
                await Task.Delay(200);
                await _dataService.SaveResultsToDatabaseAsync(Results);
                StatusText = "Результаты успешно сохранены в базу данных!";
                MessageBox.Show("Результаты успешно сохранены в базу данных!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении в БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "Ошибка при сохранении в БД";
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
                StatusText = "Загрузка данных из базы...";

                var results = await _dataService.LoadResultsFromDatabaseAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Results.Clear();
                    foreach (var item in results)
                        Results.Add(item);
                });

                if (results.Any())
                {
                    StatusText = $"Загружено {results.Count} записей из БД.";
                    MessageBox.Show($"Загружено {results.Count} записей из базы данных.", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusText = "База данных пуста.";
                    MessageBox.Show("В базе данных пока нет сохранённых результатов.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте из базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "Ошибка при экспорте из БД.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SaveToJsonAsync()
        {
            if (IsBusy || !Results.Any())
            {
                MessageBox.Show("Нет результатов для сохранения в JSON", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsBusy = true;
                await _dataService.SaveResultsToJsonAsync(Results);
                MessageBox.Show("Результаты сохранены в JSON файл", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении результатов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
        public void ClearResults()
        {
            if (IsBusy) return;

            Results.Clear();
            ProgressValue = 0;
            StatusText = "Готов к тестированию...";
        }
    }
}