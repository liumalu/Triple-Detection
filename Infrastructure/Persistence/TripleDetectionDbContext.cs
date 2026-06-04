using System.Data.Entity;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Infrastructure.Persistence
{

public class TripleDetectionDbContext : DbContext
{
    private readonly string _connectionString;

    public TripleDetectionDbContext(string connectionString)
    {
        _connectionString = connectionString;
        Database.SetInitializer<TripleDetectionDbContext>(null);
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProdTask> ProdTasks { get; set; }
    public DbSet<DetectionRecord> DetectionRecords { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.Configurations.AddFromAssembly(typeof(TripleDetectionDbContext).Assembly);
    }
}
}