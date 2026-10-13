using Netptune.Core.Models.Search;

namespace Netptune.Core.Models.ProjectTasks;

public sealed record TaskTextSearch
{
    public required TextPattern Pattern { get; init; }

    public int? ProjectId { get; init; }

    public bool InName { get; init; }

    public bool InDescription { get; init; }

    public bool InComments { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
