namespace Netptune.Core.Models.Automations;

public sealed record ScheduledActionRetry
{
    public required int ActionId { get; init; }

    public required Guid ClaimId { get; init; }

    public required DateTime ExecuteAt { get; init; }

    public required string Error { get; init; }
}
