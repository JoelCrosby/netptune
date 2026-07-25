using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.Models.Automations;

public sealed record AutomationActionPlanningContext
{
    public required AutomationRule Rule { get; init; }

    public required AutomationAction Action { get; init; }

    public required ProjectTask Task { get; init; }

    public required string ActorUserId { get; init; }

    public string? InitiatingUserId { get; init; }
}

public sealed record AutomationActionPlanContribution
{
    public AutomationNotificationContribution? Notification { get; init; }

    public AutomationFlagContribution? Flag { get; init; }

    public AutomationTaskUpdateContribution? TaskUpdate { get; init; }

    public string? CommentBody { get; init; }

    public AutomationTaskDeletionContribution? TaskDeletion { get; init; }

    public AutomationTaskCreationContribution? TaskCreation { get; init; }

    public AutomationRelationContribution? Relation { get; init; }
}

public sealed record AutomationNotificationContribution
{
    public required EventRecord Activity { get; init; }

    public string? Message { get; init; }

    public required List<string> RecipientUserIds { get; init; }

    public bool IncludeProjectMembers { get; init; }

    public List<WorkspaceRole> RecipientRoles { get; init; } = [];
}

public sealed record AutomationFlagContribution(string Name, string Description);

public sealed record AutomationTaskUpdateContribution
{
    public int? StatusId { get; init; }

    public TaskPriority? Priority { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public bool ClearDescription { get; init; }

    public string? OwnerId { get; init; }

    public bool ClearOwner { get; init; }

    public List<string>? AssigneeIds { get; init; }

    public List<string> AddTags { get; init; } = [];

    public List<string> RemoveTags { get; init; } = [];

    public AutomationDateUpdate? StartDate { get; init; }

    public AutomationDateUpdate? DueDate { get; init; }

    public EstimateType? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }

    public bool ClearEstimate { get; init; }

    public int? SprintId { get; init; }

    public bool ClearSprint { get; init; }

    public int? BoardGroupId { get; init; }
}

public sealed record AutomationTaskDeletionContribution(TimeSpan Delay);

public sealed record AutomationTaskCreationContribution
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public int? StatusId { get; init; }

    public TaskPriority? Priority { get; init; }

    public List<string> AssigneeIds { get; init; } = [];

    public List<string> AddTags { get; init; } = [];

    public DateOnly? StartDate { get; init; }

    public DateOnly? DueDate { get; init; }

    public int? SprintId { get; init; }

    public int? BoardGroupId { get; init; }

    public int? LinkRelationTypeId { get; init; }
}

public sealed record AutomationRelationContribution
{
    public AutomationRelationOperation Operation { get; init; }

    public AutomationRelationDirection Direction { get; init; }

    public int RelationTypeId { get; init; }

    public int? RelatedTaskId { get; init; }
}
