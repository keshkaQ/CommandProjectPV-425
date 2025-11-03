namespace CommandProjectPV_425.Models
{
    public class BenchmarkResult
    {
        public required string TaskType { get; set; }
        public required int DataSize { get; set; }
        public required string MethodName { get; set; }
        public required string ExecutionTime { get; set; }
        public required string MemoryUsed { get; set; }
        public required string Result { get; set; }
        public required string Speedup { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Error { get; set; }
        public required string StdDev { get; set; }
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
