using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;

namespace CommandProjectPV_425.Models
{
    // класс для работы с базой данных
    public class AppDbContext : DbContext
    {
        // таблица результатов
        public DbSet<BenchmarkResult> BenchmarkResults { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            try
            {

                // проблема сохранения в bin здесь
                var projectFolder = Environment.CurrentDirectory;                 // Текущая директория проекта (рабочая директория)
                var databaseFolder = Path.Combine(projectFolder, "DataBase");

                if (!Directory.Exists(databaseFolder))
                {
                    Directory.CreateDirectory(databaseFolder);
                }

                var dbPath = Path.Combine(databaseFolder, "benchmark.db");
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
                entity.Property(e => e.MethodName).HasMaxLength(150);
                entity.Property(e => e.TaskType).HasMaxLength(150);
                entity.Property(e => e.ExecutionTime).HasMaxLength(50);
                entity.Property(e => e.Speedup).HasMaxLength(50);
            });
        }
    }
}
