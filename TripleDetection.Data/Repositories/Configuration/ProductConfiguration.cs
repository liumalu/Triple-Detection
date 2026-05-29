using System.Data.Entity.ModelConfiguration;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    /// <summary>
    /// Product 实体配置
    /// </summary>
    public class ProductConfiguration : EntityTypeConfiguration<Product>
    {
        public ProductConfiguration()
        {
            ToTable("Products");

            HasKey(p => p.Id);
            Property(p => p.Id).HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.DatabaseGeneratedOption.Identity);

            Property(p => p.Code).IsRequired().HasMaxLength(50);
            Property(p => p.Name).IsRequired().HasMaxLength(200);
            Property(p => p.Description).HasMaxLength(1000);
            Property(p => p.SolFilePath).HasMaxLength(500);

            // 软删除查询过滤器
            HasQueryFilter(p => !p.IsDeleted);
        }
    }
}