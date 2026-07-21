namespace Netptune.Core.Requests;

public class PageRequest
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }

    public Pagination GetPagination(int maxPageSize = PaginationDefaults.MaxPageSize)
    {
        var page = Math.Max(Page ?? PaginationDefaults.DefaultPage, 1);
        var pageSize = Math.Clamp(PageSize ?? PaginationDefaults.DefaultPageSize, 1, maxPageSize);

        return new Pagination(page, pageSize);
    }
}

public readonly record struct Pagination
{
    internal Pagination(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (int)Math.Min((long)(Page - 1) * PageSize, int.MaxValue);
}
