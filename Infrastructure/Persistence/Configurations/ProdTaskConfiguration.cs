using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Infrastructure.Persistence.Configurations;

public class ProdTaskConfiguration : IEntityTypeConfiguration<ProdTask>
{
    public void Configure(EntityTypeBuilder<ProdTask> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200);
        builder.Property(t => t.CreatedBy).HasMaxLength(100);
        builder.Property(t => t.ReviewedBy).HasMaxLength(100);
        builder.Property(t => t.BatchNumber).HasMaxLength(50);
        builder.HasOne(t => t.Product)
            .WithMany()
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}