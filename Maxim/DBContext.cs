using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
namespace Maxim
{
    public class AppDbContext : DbContext
    {
        private readonly string _provider;

        public DbSet<BenchmarkResult> BenchmarkResults { get; set; }

        public AppDbContext(string provider = "SQLite")
        {
            _provider = provider;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_provider == "SQLite")
            {
                // Папка Data будет создана рядом с exe-файлом, в нее и будет сохранятся база!
                var dbFolder = Path.Combine(AppContext.BaseDirectory, "Data");
                Directory.CreateDirectory(dbFolder);

                var dbPath = Path.Combine(dbFolder, "benchmark.db");
              
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
            else if (_provider == "MSSQL")
            {
                //Подключение к MS SQL Server (на будущее)
                optionsBuilder.UseSqlServer(
                    @"Server=HOME\SQLEXPRESS;Database=BenchmarkDB;Trusted_Connection=True;TrustServerCertificate=True;");
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
                entity.Property(e => e.Result).HasMaxLength(50);
            });
        }
    }
}
