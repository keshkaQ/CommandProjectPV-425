using CommandProjectPV_425_Test.Models;

namespace CommandProjectPV_425_Test.Interfaces
{
    // контракт для работы с данными (сохранение в бд, загрузка из бд, сохранение в json)
    // отделяет логику хранения данных от бизнес-логики
    public interface IDataService
    {
        Task SaveResultsToDatabaseAsync(IEnumerable<BenchmarkResult> results);
        Task<List<BenchmarkResult>> LoadResultsFromDatabaseAsync();
        Task SaveResultsToJsonAsync(IEnumerable<BenchmarkResult> results);
    }
}
