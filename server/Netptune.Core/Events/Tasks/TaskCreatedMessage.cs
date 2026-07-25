using Netptune.Core.Enums;

namespace Netptune.Core.Events.Tasks;

public sealed record TaskCreatedMessage : IEventMessage
{
    public static string Subject => MessageKeys.Subjects.Automation;

    public Guid EventId { get; init; } = Guid.NewGuid();

    public int WorkspaceId { get; init; }

    public int TaskId { get; init; }

    public string ActorUserId { get; init; } = null!;

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public EventOriginType OriginType { get; init; }

    public Guid? CorrelationId { get; init; }

    public int? AutomationRuleId { get; init; }

    public int? AutomationRunId { get; init; }

    public int ChainDepth { get; init; }
}
