using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;

namespace CommandProjectPV_425.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<BenchmarkResult> BenchmarkResults { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            try
            {
                // Папка Data будет создана рядом с exe-файлом, в нее и будет сохранятся база!
                var dbFolder = Path.Combine(AppContext.BaseDirectory, "Data");
                if (!Directory.Exists(dbFolder))
                {
                    Directory.CreateDirectory(dbFolder);
                    Debug.WriteLine($"Создана папка для базы данных: {dbFolder}");
                }

                var dbPath = Path.Combine(dbFolder, "benchmark.db");
                Debug.WriteLine($"Путь к базе данных: {dbPath}");

                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка инициализации базы данных: {ex.Message}");
                throw;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BenchmarkResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Processor).HasMaxLength(200);
                entity.Property(e => e.OperatingSystem).HasMaxLength(100);
                entity.Property(e => e.MethodName).HasMaxLength(150);
                entity.Property(e => e.TaskType).HasMaxLength(150);
                entity.Property(e => e.ExecutionTime).HasMaxLength(50);
                entity.Property(e => e.Speedup).HasMaxLength(50);
            });
        }
    }
}
