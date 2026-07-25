using Netptune.Core.Enums;

namespace Netptune.Core.Events.Sprints;

public sealed record SprintLifecycleMessage : IEventMessage
{
    public static string Subject => MessageKeys.Subjects.Automation;

    public Guid EventId { get; init; } = Guid.NewGuid();

    public int WorkspaceId { get; init; }

    public int SprintId { get; init; }

    public SprintLifecycleState State { get; init; }

    public string ActorUserId { get; init; } = null!;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
