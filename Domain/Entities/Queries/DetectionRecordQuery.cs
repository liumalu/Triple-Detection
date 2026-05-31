namespace TripleDetection.Domain.Entities.Queries;

public class DetectionRecordQuery : PagedQuery
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? TaskId { get; set; }
    public int? ProductId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public bool? IsOK { get; set; }
}