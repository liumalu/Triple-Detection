namespace TripleDetection.Domain.Entities
{

public class AuditLog : BaseEntity
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public int ObjectId { get; set; }
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;

    // NEW: For efficient SQL queries on status transitions
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;

    // NEW: Link to related records (e.g., DetectionRecord.Id)
    public int? RelatedRecordId { get; set; }
}
}