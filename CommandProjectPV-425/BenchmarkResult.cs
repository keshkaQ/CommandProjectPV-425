namespace CommandProjectPV_425
{
    public class BenchmarkResult
    {
        public string TaskType { get; set; }
        public int DataSize { get; set; }
        public string MethodName { get; set; }
        public string ExecutionTime { get; set; }
        public string MemoryUsed { get; set; }
        public string Result { get; set; }
        public string Speedup { get; set; }
        public DateTime Timestamp { get; set; }
        public string Error { get; set; }
        public string StdDev { get; set; }
        public List<double> RawTimes { get; set; }

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
