using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommandProjectPV_425.Services;

public class DataService : IDataService
{
    public async Task SaveResultsToDatabaseAsync(IEnumerable<BenchmarkResult> results)
    {
        // создаем контекст базы данных
        using var context = new AppDbContext();

        // гарантируем что база данных и таблицы созданы
        await context.Database.EnsureCreatedAsync();

        // добавляем все результаты в контекст
        await context.BenchmarkResults.AddRangeAsync(results);

        // сохраняем изменения в базе данных
        await context.SaveChangesAsync();
    }

    public async Task<List<BenchmarkResult>> LoadResultsFromDatabaseAsync()
    {
        // создаем контекст базы данных
        using var context = new AppDbContext();

        // загружаем все результаты из БД, предварительно отсортированные
        return await context.BenchmarkResults.OrderByDescending(r => r.Timestamp).ToListAsync();
    }

    public async Task SaveResultsToJsonAsync(IEnumerable<BenchmarkResult> results)
    {
        // путь к папке для сохранения результатов
        string directoryPath = "BenchmarkResults";

        // создаем папку, если она не существует
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        // генерируем имя файла со временем
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"BenchmarkResults_{timestamp}.json";
        string filePath = Path.Combine(directoryPath, fileName);

        // преобразуем результаты для сериализации в json
        var resultsToSave = results.Select(r => new
        {
            r.TaskType,
            r.DataSize,
            r.MethodName,
            r.ExecutionTime,
            r.Speedup,
            Timestamp = r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            r.RawTimes
        }).ToList();

        // настройки сериализации JSON
        var options = new JsonSerializerOptions
        {
            WriteIndented = true, // отступы
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // camelCase 
        };

        // сериализуем данные в JSON строку и записываем в файл
        string json = JsonSerializer.Serialize(resultsToSave, options);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<List<BenchmarkResult>> LoadResultsFromJsonAsync(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON строка не может быть пустой");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new DateTimeConverter() }
            };
            var results = JsonSerializer.Deserialize<List<BenchmarkResult>>(json, options);
            return results.OrderBy(r =>
            {
                var timeStr = r.ExecutionTime.Replace(" ms", "").Replace(",", ".");
                if (double.TryParse(timeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double timeMs))
                    return timeMs;
                return double.MaxValue;
            }).ToList() ?? new List<BenchmarkResult>();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Ошибка формата JSON: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка загрузки данных из JSON: {ex.Message}", ex);
        }
    }

    public async Task<(List<int> coreCounts, List<string> taskNames, List<int> dataSizes)> LoadAnalyticsDataAsync()
    {
        using var context = new AppDbContext();

        var coreCountsTask = context.BenchmarkResults
            .Select(r => r.CoreCount) // Выбираем только значения CoreCount
            .Distinct()              // Убираем дубликаты, оставляя только уникальные
            .OrderBy(count => count) // Сортируем по возрастанию (по желанию)
            .ToListAsync();

        var taskNamesTask = context.BenchmarkResults
            .Select(n => n.TaskType)
            .Distinct()
            //.OrderBy(taskType => taskType)
            .ToListAsync();

        var dataSizesTask = context.BenchmarkResults
            .Select(n => n.DataSize)
            .Distinct()
            .OrderBy(dataSize => dataSize)
            .ToListAsync();

        await Task.WhenAll(coreCountsTask, taskNamesTask, dataSizesTask);

        return (await coreCountsTask, await taskNamesTask, await dataSizesTask);
    }

    private class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                if (DateTime.TryParse(dateString, out DateTime date))
                {
                    return date;
                }
                if (DateTime.TryParseExact(dateString, "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
            }
            return DateTime.Now;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}