using Netptune.Core.Enums;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.ViewModels.Roadmap;

public sealed record RoadmapViewModel
{
    public required DateOnly From { get; init; }

    public required DateOnly To { get; init; }

    public IReadOnlyList<RoadmapTaskViewModel> Tasks { get; init; } = [];

    public IReadOnlyList<RoadmapRelationViewModel> Relations { get; init; } = [];

    public IReadOnlyList<RoadmapSprintViewModel> Sprints { get; init; } = [];

    public bool Truncated { get; init; }
}

public sealed record RoadmapTaskViewModel
{
    public int Id { get; init; }

    public int ProjectScopeId { get; init; }

    public required string SystemId { get; init; }

    public required string Name { get; init; }

    public int ProjectId { get; init; }

    public required string ProjectName { get; init; }

    public required string ProjectKey { get; init; }

    public int StatusId { get; init; }

    public required string StatusName { get; init; }

    public required string StatusKey { get; init; }

    public string? StatusColor { get; init; }

    public StatusCategory StatusCategory { get; init; }

    public TaskPriority? Priority { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? DueDate { get; init; }

    public int? SprintId { get; init; }

    public IReadOnlyList<AssigneeViewModel> Assignees { get; init; } = [];
}

public sealed record RoadmapRelationViewModel
{
    public int Id { get; init; }

    public int SourceTaskId { get; init; }

    public int TargetTaskId { get; init; }

    public int RelationTypeId { get; init; }

    public required string RelationTypeKey { get; init; }

    public RelationCategory Category { get; init; }
}

public sealed record RoadmapSprintViewModel
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public SprintStatus Status { get; init; }

    public int ProjectId { get; init; }
}
