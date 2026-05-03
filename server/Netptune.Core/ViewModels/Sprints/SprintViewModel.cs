using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Sprints;

public record SprintViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Goal { get; init; }

    public SprintStatus Status { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public DateTime? CompletedAt { get; init; }

    public int ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    public string ProjectKey { get; init; } = null!;

    public int WorkspaceId { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public int TaskCount { get; init; }
}
