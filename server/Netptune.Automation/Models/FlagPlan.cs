namespace Netptune.Automation.Models;

internal sealed record FlagPlan
{
    public required PendingAutomationExecution Execution { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }
}
