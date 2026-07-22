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

    public bool DeleteTask { get; init; }
}

public sealed record AutomationNotificationContribution(EventRecord Activity, List<string> RecipientUserIds);

public sealed record AutomationFlagContribution(string Name, string Description);

public sealed record AutomationTaskUpdateContribution(int? StatusId, TaskPriority? Priority);
