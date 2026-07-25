namespace Netptune.Core.Models.Automations;

public sealed record AutomationManualRunRequest
{
    public int RuleId { get; init; }

    public int WorkspaceId { get; init; }

    public required IReadOnlyCollection<int> TaskIds { get; init; }

    public required string InitiatingUserId { get; init; }
}

public sealed record AutomationManualRunResult
{
    public int ExecutedCount { get; init; }

    public int SkippedCount { get; init; }

    public bool RuleFound { get; init; }

    public static AutomationManualRunResult NotFound { get; } = new();
}
