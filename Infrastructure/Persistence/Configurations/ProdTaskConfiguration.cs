using System.Data.Entity.ModelConfiguration;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Infrastructure.Persistence.Configurations
{

public class ProdTaskConfiguration : EntityTypeConfiguration<ProdTask>
{
    public ProdTaskConfiguration()
    {
        ToTable("Tasks");
        HasKey(t => t.Id);
        Property(t => t.Name).HasMaxLength(200);
        Property(t => t.CreatedBy).HasMaxLength(100);
        Property(t => t.ReviewedBy).HasMaxLength(100);
        Property(t => t.BatchNumber).HasMaxLength(50);
        Property(t => t.ProductId).IsRequired();
    }
}
}