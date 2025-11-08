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

    public async Task<List<int>> GetCoreCounts()
    {
        using var context = new AppDbContext();

        return await context.BenchmarkResults
            .Select(r => r.CoreCount) // Выбираем только значения CoreCount
            .Distinct()              // Убираем дубликаты, оставляя только уникальные
            .OrderBy(count => count) // Сортируем по возрастанию (по желанию)
            .ToListAsync();
    }

    public async Task<List<string>> GetTasksNames()
    {
        using var context = new AppDbContext();

        return await context.BenchmarkResults
            .Select(n => n.TaskType)
            .Distinct()
            //.OrderBy(taskType => taskType)
            .ToListAsync();
    }

    public async Task<List<int>> GetDataSizes()
    {
        using var context = new AppDbContext();

        return await context.BenchmarkResults
            .Select(n => n.DataSize)
            .Distinct()
            .OrderBy(dataSize => dataSize)
            .ToListAsync();
    }
}
