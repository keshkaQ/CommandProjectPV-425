using CommandProjectPV_425.Models;
using CommandProjectPV_425.Services;

namespace CommandProjectPV_425.Interfaces
{
    public interface IAnalyticService
    {
        List<MethodStatistic> CalculateAverageTimePerMethod(IEnumerable<BenchmarkResult> results);
    }
}
