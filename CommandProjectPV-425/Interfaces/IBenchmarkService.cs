using CommandProjectPV_425.Models;

namespace CommandProjectPV_425.Interfaces
{
    // контракт для бенчмарков, отделяет логику запуска бенчмарков от UI
    public interface IBenchmarkService
    {
        Task<List<BenchmarkResult>> RunBenchmarkAsync(string taskType, int size);
        void SetBenchmarkSize(Type benchmarkType, int size);
        string FormatMethodName(string methodName);
    }
}
