using System.Data.Entity.ModelConfiguration;
using TripleDetection.Data.Entities;

namespace TripleDetection.Data.Repositories.Configuration
{
    /// <summary>
    /// DetectionRecord 实体配置
    /// </summary>
    public class DetectionRecordConfiguration : EntityTypeConfiguration<DetectionRecord>
    {
        public DetectionRecordConfiguration()
        {
            ToTable("DetectionRecords");

            HasKey(d => d.Id);

            Property(d => d.BatchNumber).HasMaxLength(50);
            Property(d => d.CodeInfo).HasMaxLength(200);
            Property(d => d.ImagePath).HasMaxLength(500);
            Property(d => d.IsOK).IsRequired();
            Property(d => d.ElapsedMs).IsRequired();
            Property(d => d.ProductId).IsRequired();

            // 关系：DetectionRecord -> Task
            HasRequired(d => d.Task)
                .WithMany()
                .HasForeignKey(d => d.TaskId)
                .WillCascadeOnDelete(false);
        }
    }
}