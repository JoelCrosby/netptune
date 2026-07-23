namespace Netptune.Core.Models.Automations;

public sealed record ScheduledActionClaim
{
    public required DateTime DueBefore { get; init; }

    public required DateTime LeaseExpiresAt { get; init; }

    public required Guid ClaimId { get; init; }

    public required int BatchSize { get; init; }
}
