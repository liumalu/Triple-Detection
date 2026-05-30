using System.Data.Entity.ModelConfiguration;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    /// <summary>
    /// Task 实体配置
    /// </summary>
    public class TaskConfiguration : EntityTypeConfiguration<Data.Entities.ProdTask>
    {
        public TaskConfiguration()
        {
            ToTable("Tasks");

            HasKey(t => t.Id);

            Property(t => t.Name).IsRequired().HasMaxLength(200);
            Property(t => t.BatchNumber).HasMaxLength(50);
            Property(t => t.CreatedBy).HasMaxLength(100);
            Property(t => t.ReviewedBy).HasMaxLength(100);

            // 关系：Task -> Product
            HasRequired(t => t.Product)
                .WithMany()
                .HasForeignKey(t => t.ProductId)
                .WillCascadeOnDelete(false);
        }
    }
}