using System;

namespace TripleDetection.Domain.Entities
{

public class DetectionRecord : BaseEntity
{
    public int TaskId { get; set; }
    public int ProductId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public bool IsOK { get; set; }
    public string ProductionDate { get; set; } = string.Empty;
    public string ExpirationDate { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public long ElapsedMs { get; set; }
    public DateTime DetectionTime { get; set; }

    // NEW: Denormalized for efficient statistics queries without joins
    public string TaskName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
}
}