using System.Data.Entity.ModelConfiguration;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Infrastructure.Persistence.Configurations
{

public class DetectionRecordConfiguration : EntityTypeConfiguration<DetectionRecord>
{
    public DetectionRecordConfiguration()
    {
        ToTable("DetectionRecords");
        HasKey(d => d.Id);
        Property(d => d.BatchNumber).HasMaxLength(50);
        Property(d => d.ProductionDate).HasMaxLength(50);
        Property(d => d.ExpirationDate).HasMaxLength(50);
        Property(d => d.ImagePath).HasMaxLength(500);
        Property(d => d.TaskId).IsRequired();
    }
}
}