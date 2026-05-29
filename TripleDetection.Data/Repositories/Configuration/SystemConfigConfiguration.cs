using System.Data.Entity.ModelConfiguration;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    /// <summary>
    /// SystemConfig 实体配置
    /// </summary>
    public class SystemConfigConfiguration : EntityTypeConfiguration<SystemConfig>
    {
        public SystemConfigConfiguration()
        {
            ToTable("SystemConfigs");

            HasKey(s => s.Id);
            Property(s => s.Id).HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.DatabaseGeneratedOption.Identity);

            Property(s => s.Category).IsRequired().HasMaxLength(50);
            Property(s => s.Key).IsRequired().HasMaxLength(100);
            Property(s => s.Value).HasMaxLength(1000);
            Property(s => s.Description).HasMaxLength(500);

            HasQueryFilter(s => !s.IsDeleted);
        }
    }
}