using CommandProjectPV_425.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace CommandProjectPV_425.Interfaces
{
    // контракт для графиков, отделяет логику данных от отображения
    public interface IChartService
    {
        (List<string> labels, List<double> timeValues, List<double> speedupValues) PrepareChartData(IEnumerable<BenchmarkResult> results);

        (ISeries[] Series, Axis[] XAxes, Axis[] YAxes) CreateColumnChart(
        List<string> labels,
        List<double> values,
        string xAxisName,
        string yAxisName,
        Func<double, string> yLabelFormatter,
        Func<int, double, string> tooltipFormatter,
        bool isSpeedupChart = false);

        (Axis[] X, Axis[] Y) CreateAxes(
            string xName,
            string yName,
            List<double> values,
            Func<double, string> yLabelFormatter,
            bool isSpeedupChart = false);

        List<MethodStatistic> CalculateAverageTimePerMethod(IEnumerable<BenchmarkResult> results);

        string GetTaskNameDescription(string taskName);
    }
}
