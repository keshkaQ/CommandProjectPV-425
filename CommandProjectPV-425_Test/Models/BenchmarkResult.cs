using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommandProjectPV_425_Test.Models
{
    public class BenchmarkResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Processor { get; set; }
        public required int CoreCount { get; set; }
        public required string OperatingSystem { get; set; }
        public required string TaskType { get; set; }
        public required int DataSize { get; set; }
        public required string MethodName { get; set; }
        public required string ExecutionTime { get; set; }
        public required string Speedup { get; set; }
        public DateTime Timestamp { get; set; }
        public required List<double> RawTimes { get; set; }
    }
}
