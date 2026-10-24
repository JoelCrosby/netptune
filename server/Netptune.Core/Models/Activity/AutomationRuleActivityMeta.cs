namespace Netptune.Core.Models.Activity;

public sealed record AutomationRuleActivityMeta
{
    public required string Name { get; init; }

    public List<string>? ChangedSections { get; init; }

    public int? CancelledScheduledActions { get; init; }

    public int? ClonedFromRuleId { get; init; }

    public string? Reason { get; init; }

    public static int? CancelledCount(int count)
    {
        return count > 0 ? count : null;
    }
}
