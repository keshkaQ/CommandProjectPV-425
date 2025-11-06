using CommandProjectPV_425.Interfaces;
using CommandProjectPV_425.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;

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

    public async Task<List<MethodStatistic>> GetAverageTimePerMethodAsync()
    {
        using var context = new AppDbContext(); // Замените на ваш контекст БД

        // 1. Загружаем все результаты
        var allResults = await context.BenchmarkResults.ToListAsync();

        // 2. Группируем по имени метода и рассчитываем среднее время
        var statistics = allResults
            .GroupBy(r => r.MethodName)
            .Select(g =>
            {
                // Берем среднее значение из всех ExecutionTime, конвертируя их из строки в double (мс)
                var validResults = g.Where(r => r.ExecutionTime != "Failed");

                // ВАЖНО: ParseTimeToMs должен возвращать double (мс)
                var averageTime = validResults.Any()
                    ? validResults.Average(r => Helpers.DataParser.ParseTimeToMs(r.ExecutionTime))
                    : 0.0;

                return new MethodStatistic
                {
                    MethodName = g.Key,
                    AverageTimeMs = averageTime,
                    AverageSpeedup = 0.0
                };
            })
            .Where(s => s.AverageTimeMs > 0) // Удаляем методы, у которых нет успешных результатов
            .OrderBy(s => s.AverageTimeMs)
            .ToList();

        return statistics;
    }
}

public class MethodStatistic
{
    public string MethodName { get; set; }
    public double AverageTimeMs { get; set; }
    public double AverageSpeedup { get; set; }
}
