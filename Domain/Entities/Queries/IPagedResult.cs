namespace TripleDetection.Domain.Entities.Queries;

public interface IPagedResult<T>
{
    IEnumerable<T> Items { get; }
    int TotalCount { get; }
    int PageIndex { get; }
    int PageSize { get; }
    int TotalPages { get; }
    bool HasPreviousPage { get; }
    bool HasNextPage { get; }
}

public class PagedResult<T> : IPagedResult<T>
{
    public IEnumerable<T> Items { get; }
    public int TotalCount { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public PagedResult(IEnumerable<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}