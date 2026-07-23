using Netptune.Core.Enums;

namespace Netptune.Core.Models.Automations;

public sealed record ScheduledActionCompletion
{
    public required int ActionId { get; init; }

    public required Guid ClaimId { get; init; }

    public required ScheduledAutomationActionStatus Status { get; init; }

    public required DateTime ProcessedAt { get; init; }

    public string? Error { get; init; }
}
