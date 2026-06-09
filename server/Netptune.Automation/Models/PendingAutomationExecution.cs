using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record PendingAutomationExecution
{
    public required AutomationRule Rule { get; init; }

    public required ProjectTask Task { get; init; }

    public required string ActorUserId { get; init; }

    public required string IdempotencyKey { get; init; }
}
