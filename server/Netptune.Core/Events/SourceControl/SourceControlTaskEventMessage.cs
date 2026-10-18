using Netptune.Core.Enums;

namespace Netptune.Core.Events.SourceControl;

public sealed record SourceControlTaskEventMessage : IEventMessage
{
    public static string Subject => MessageKeys.Subjects.Automation;

    // The source control event's own id, which is the same on every redelivery of the webhook.
    public required Guid EventId { get; init; }

    public int WorkspaceId { get; init; }

    public AutomationTriggerType TriggerType { get; init; }

    public required IReadOnlyList<int> TaskIds { get; init; }

    public string? TargetBranch { get; init; }

    public bool IsDraft { get; init; }

    public bool IsReadyForReview { get; init; }

    public string? ActorUserId { get; init; }

    public DateTime OccurredAt { get; init; }
}
