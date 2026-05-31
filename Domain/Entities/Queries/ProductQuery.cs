namespace TripleDetection.Domain.Entities.Queries;

public class ProductQuery : PagedQuery
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? Status { get; set; }
    public DateTime? CreateAtFrom { get; set; }
    public DateTime? CreateAtTo { get; set; }
}