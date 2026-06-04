using System;

namespace TripleDetection.Domain.Entities.Queries
{

public class AuditLogQuery : PagedQuery
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public string Keyword { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}
}