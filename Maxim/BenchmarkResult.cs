using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maxim
{
    public class BenchmarkResult
    {
        [Key]
        public int Id { get; set; }

        public required string Processor { get; set; }
        public required int CoreCount { get; set; }
        public required string OperatingSystem { get; set; }
        public required string TaskType { get; set; }
        public required int DataSize { get; set; }
        public required string MethodName { get; set; }
        public required string ExecutionTime { get; set; }
        public required string Result { get; set; }
        public required string Speedup { get; set; }
        public DateTime Timestamp { get; set; }

        public required List<double> RawTimes { get; set; }

        public static (double mean, double error, double stdDev) CalculateStatistics(List<double> measurements)
        {
            if (measurements == null || measurements.Count < 2)
                return (measurements?.FirstOrDefault() ?? 0, 0, 0);

            var mean = measurements.Average();
            var stdDev = Math.Sqrt(measurements.Sum(x => Math.Pow(x - mean, 2)) / (measurements.Count - 1));
            var error = stdDev / Math.Sqrt(measurements.Count);

            return (mean, error, stdDev);
        }
    }
}
