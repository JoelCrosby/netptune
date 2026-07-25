using Netptune.Core.Enums;

namespace Netptune.Core.Events.Relations;

public sealed record TaskRelationChangedMessage : IEventMessage
{
    public static string Subject => MessageKeys.Subjects.Automation;

    public Guid EventId { get; init; } = Guid.NewGuid();

    public int WorkspaceId { get; init; }

    public int SourceTaskId { get; init; }

    public int TargetTaskId { get; init; }

    public RelationCategory Category { get; init; }

    public TaskRelationChange Change { get; init; }

    public string ActorUserId { get; init; } = null!;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
