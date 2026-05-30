using System;

namespace TripleDetection.Data.Entities
{
    public class DetectionRecord : BaseEntity
    {
        public int TaskId { get; set; }
        public int ProductId { get; set; }
        public string BatchNumber { get; set; }
        public bool IsOK { get; set; }
        public string ProductionDate { get; set; }
        public string ExpirationDate { get; set; }
        public string ImagePath { get; set; }
        public long ElapsedMs { get; set; }
        public DateTime DetectionTime { get; set; }

        public virtual ProdTask Task { get; set; }
    }
}
