namespace EventHub.Application.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class PagingQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public int Skip => (Math.Max(Page, 1) - 1) * Take;
    public int Take => PageSize is < 1 or > 50 ? 10 : PageSize;
}
