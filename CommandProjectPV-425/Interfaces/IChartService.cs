using CommandProjectPV_425.Models;

namespace CommandProjectPV_425.Interfaces
{
    // контракт для графиков, отделяет логику данных от отображения
    public interface IChartService
    {
        (List<string> labels, List<double> timeValues, List<double> speedupValues) PrepareChartData(IEnumerable<BenchmarkResult> results);
    }
}
