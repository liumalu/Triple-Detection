using System.Data.Entity.ModelConfiguration;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Infrastructure.Persistence.Configurations
{

public class AuditLogConfiguration : EntityTypeConfiguration<AuditLog>
{
    public AuditLogConfiguration()
    {
        ToTable("AuditLogs");
        HasKey(a => a.Id);
        Property(a => a.UserName).HasMaxLength(200);
        Property(a => a.Action).HasMaxLength(50);
        Property(a => a.ObjectType).HasMaxLength(50);
        Property(a => a.Details).HasMaxLength(1000);
        Property(a => a.IpAddress).HasMaxLength(50);
    }
}
}