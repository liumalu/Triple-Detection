using System.Data.Entity.ModelConfiguration;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Infrastructure.Persistence.Configurations
{

public class SystemConfigConfiguration : EntityTypeConfiguration<SystemConfig>
{
    public SystemConfigConfiguration()
    {
        ToTable("SystemConfigs");
        HasKey(s => s.Id);
        Property(s => s.Category).HasMaxLength(100);
        Property(s => s.Key).HasMaxLength(100);
        Property(s => s.Value).HasMaxLength(1000);
        Property(s => s.Description).HasMaxLength(500);
    }
}
}