namespace CommandProjectPV_425.Models
{
    public class BenchmarkResult
    {
        public required string TaskType { get; set; }
        public required int DataSize { get; set; }
        public required string MethodName { get; set; }
        public required string ExecutionTime { get; set; }
        public required string Speedup { get; set; }
        public string Error { get; set; }
        public DateTime Timestamp { get; set; }
        public required List<double> RawTimes { get; set; }
    }
}
