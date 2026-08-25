using Netptune.Core.Requests;

namespace Netptune.Api.Requests;

public sealed record PublicBoardViewFilter
{
    public string[] Assignees { get; init; } = [];

    public string[] Tags { get; init; } = [];

    public int[] StatusIds { get; init; } = [];

    public string? Search { get; init; }

    public int? SprintId { get; init; }

    public bool? HasTags { get; init; }

    internal BoardGroupsFilter ToBoardGroupsFilter()
    {
        return new BoardGroupsFilter
        {
            Users = Assignees,
            Tags = Tags,
            StatusIds = StatusIds,
            Term = Search,
            SprintId = SprintId,
            HasTags = HasTags,
        };
    }
}
