namespace Netptune.Core.Models.Roadmap;

public sealed record RoadmapFilter
{
    public required DateOnly From { get; init; }

    public required DateOnly To { get; init; }

    public IReadOnlyCollection<int> ProjectIds { get; init; } = [];

    public IReadOnlyCollection<int> SprintIds { get; init; } = [];

    public string? Search { get; init; }

    public IReadOnlyCollection<string> Tags { get; init; } = [];

    public IReadOnlyCollection<int> StatusIds { get; init; } = [];

    public IReadOnlyCollection<string> Assignees { get; init; } = [];
}
