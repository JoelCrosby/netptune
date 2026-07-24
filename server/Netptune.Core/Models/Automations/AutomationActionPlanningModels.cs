using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.Models.Automations;

public sealed record AutomationActionPlanningContext
{
    public required AutomationRule Rule { get; init; }

    public required AutomationAction Action { get; init; }

    public required ProjectTask Task { get; init; }

    public required string ActorUserId { get; init; }
}

public sealed record AutomationActionPlanContribution
{
    public AutomationNotificationContribution? Notification { get; init; }

    public AutomationFlagContribution? Flag { get; init; }

    public AutomationTaskUpdateContribution? TaskUpdate { get; init; }

    public string? CommentBody { get; init; }

    public AutomationTaskDeletionContribution? TaskDeletion { get; init; }
}

public sealed record AutomationNotificationContribution(EventRecord Activity, List<string> RecipientUserIds);

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
