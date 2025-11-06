using CommandProjectPV_425.Models;

namespace CommandProjectPV_425.Interfaces
{
    // контракт для работы с данными (сохранение в бд, загрузка из бд, сохранение в json)
    // отделяет логику хранения данных от бизнес-логики
    public interface IDataService
    {
        Task SaveResultsToDatabaseAsync(IEnumerable<BenchmarkResult> results);
        Task<List<BenchmarkResult>> LoadResultsFromDatabaseAsync();
        Task SaveResultsToJsonAsync(IEnumerable<BenchmarkResult> results);
        Task <List<BenchmarkResult>> LoadResultsFromJsonAsync(string json);
    }
}
