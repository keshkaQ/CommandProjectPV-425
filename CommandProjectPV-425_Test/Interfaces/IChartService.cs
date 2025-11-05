using CommandProjectPV_425_Test.Models;

namespace CommandProjectPV_425_Test.Interfaces
{
    // контракт для графиков, отделяет логику данных от отображения
    public interface IChartService
    {
        (List<string> labels, List<double> timeValues, List<double> speedupValues) PrepareChartData(IEnumerable<BenchmarkResult> results);
    }
}
