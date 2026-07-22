using Netptune.Core.Enums;
using Netptune.Core.Requests;

namespace Netptune.PublicApi.Requests;

public sealed record PublicTaskFilter
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }

    public int? ProjectId { get; init; }

    public int? SprintId { get; init; }

    public string? Search { get; init; }

    public int[] StatusIds { get; init; } = [];

    public TaskPriority[] Priorities { get; init; } = [];

    public string[] Assignees { get; init; } = [];

    public string[] Tags { get; init; } = [];

    internal TaskFilter ToTaskFilter()
    {
        return new TaskFilter
        {
            Page = Page,
            PageSize = PageSize,
            SortBy = SortBy,
            SortDirection = SortDirection,
            ProjectId = ProjectId,
            SprintId = SprintId,
            Search = Search,
            StatusIds = StatusIds,
            Priorities = Priorities,
            Assignees = Assignees,
            Tags = Tags,
        };
    }
}
