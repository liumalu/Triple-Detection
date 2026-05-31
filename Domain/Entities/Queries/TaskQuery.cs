namespace TripleDetection.Domain.Entities.Queries;

public class TaskQuery : PagedQuery
{
    public string Name { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public int? Status { get; set; }
    public DateTime? ProductionDateFrom { get; set; }
    public DateTime? ProductionDateTo { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
}