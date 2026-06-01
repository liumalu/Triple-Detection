using System;
using TripleDetection.Domain.Enums;

namespace TripleDetection.Domain.Entities;

public class ProdTask : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public string CreatedBy { get; set; } = string.Empty;
    public string ReviewedBy { get; set; } = string.Empty;
    public DateTime? ReviewedAt { get; set; }
    public DateTime ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
}