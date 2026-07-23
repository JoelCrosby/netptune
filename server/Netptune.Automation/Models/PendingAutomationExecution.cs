using Netptune.Core.Entities;
using Netptune.Core.Events.Tasks;

namespace Netptune.Automation.Models;

internal sealed record PendingAutomationExecution
{
    public required AutomationRule Rule { get; init; }

    public required ProjectTask Task { get; init; }

    public required string ActorUserId { get; init; }

    public required string IdempotencyKey { get; init; }

    public required DateTime TriggeredAt { get; init; }

    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    public Guid? CausationEventId { get; init; }

    public int ChainDepth { get; init; }

    public TaskChangedMessage? TriggerMessage { get; init; }

    public AutomationRun? Run { get; set; }
}
