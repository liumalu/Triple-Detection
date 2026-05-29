using System.Data.Entity.ModelConfiguration;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    public class AuditLogConfiguration : EntityTypeConfiguration<AuditLog>
    {
        public AuditLogConfiguration()
        {
            ToTable("AuditLogs");
            HasKey(a => a.Id);
            Property(a => a.UserName).HasMaxLength(50);
            Property(a => a.Action).IsRequired().HasMaxLength(20);
            Property(a => a.ObjectType).IsRequired().HasMaxLength(20);
            Property(a => a.Details).HasMaxLength(500);
            Property(a => a.IpAddress).HasMaxLength(50);
            HasOptional(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .WillCascadeOnDelete(false);
        }
    }
}