using System;
using System.Data.Entity;
using System.IO;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories.Configuration;

namespace TripleDetection.Data.Repositories.Sqlite
{
    /// <summary>
    /// SQLite 数据库上下文
    /// </summary>
    [DbConfigurationType(typeof(SQLiteEFConfiguration))]
    public class SqliteDbContext : DbContext
    {
        private static readonly string DefaultDbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Data",
            "tripledetection.db");

        public SqliteDbContext() : base($"Data Source={DefaultDbPath}")
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public SqliteDbContext(string connectionString) : base(connectionString)
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        // DbSet 实体集
        public DbSet<Product> Products { get; set; }
        public DbSet<Data.Entities.ProdTask> Tasks { get; set; }
        public DbSet<DetectionRecord> DetectionRecords { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 加载所有实体配置
            modelBuilder.Configurations.Add(new ProductConfiguration());
            modelBuilder.Configurations.Add(new TaskConfiguration());
            modelBuilder.Configurations.Add(new DetectionRecordConfiguration());
            modelBuilder.Configurations.Add(new SystemConfigConfiguration());
            modelBuilder.Configurations.Add(new UserConfiguration());
            modelBuilder.Configurations.Add(new AuditLogConfiguration());
        }
    }
}