using System.Data.Entity.ModelConfiguration;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Infrastructure.Persistence.Configurations
{

public class ProductConfiguration : EntityTypeConfiguration<Product>
{
    public ProductConfiguration()
    {
        ToTable("Products");
        HasKey(p => p.Id);
        Property(p => p.Code).HasMaxLength(50);
        Property(p => p.Name).HasMaxLength(200);
        Property(p => p.Description).HasMaxLength(1000);
        Property(p => p.SolFilePath).HasMaxLength(500);
    }
}
}