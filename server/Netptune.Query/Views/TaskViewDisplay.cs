namespace Netptune.Query.Views;

public sealed record TaskViewDisplay
{
    public const int DefaultPageSize = 25;

    public List<TaskViewColumn> Columns { get; init; } = [];

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }

    public int PageSize { get; init; } = DefaultPageSize;
}
