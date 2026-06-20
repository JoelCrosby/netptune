namespace Netptune.Core.Events.Tasks;

public record TaskStatusChangedMessage : IEventMessage
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public int WorkspaceId { get; init; }

    public int TaskId { get; init; }

    public string ActorUserId { get; init; } = null!;

    public int OldStatusId { get; init; }

    public int NewStatusId { get; init; }

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
