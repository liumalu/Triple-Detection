namespace TripleDetection.Domain.Entities.Queries;

public class PagedQuery
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "Id";
    public bool SortDescending { get; set; } = true;
}