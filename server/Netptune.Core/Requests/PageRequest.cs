namespace Netptune.Core.Requests;

public class PageRequest
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }

    public int GetPage()
    {
        return Math.Max(Page ?? PaginationDefaults.DefaultPage, 1);
    }

    public int GetPageSize(int maxPageSize = PaginationDefaults.MaxPageSize)
    {
        return Math.Clamp(PageSize ?? PaginationDefaults.DefaultPageSize, 1, maxPageSize);
    }

    public int GetSkip()
    {
        return (GetPage() - 1) * GetPageSize();
    }
}
