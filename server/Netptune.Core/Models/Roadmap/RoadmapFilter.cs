namespace Netptune.Core.Models.Roadmap;

public sealed record RoadmapFilter
{
    public required DateOnly From { get; init; }

    public required DateOnly To { get; init; }

    public IReadOnlyCollection<int> ProjectIds { get; init; } = [];

    public IReadOnlyCollection<int> SprintIds { get; init; } = [];
}
