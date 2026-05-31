namespace TripleDetection.Domain.Entities;

public class AuditLog : BaseEntity
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public int ObjectId { get; set; }
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}