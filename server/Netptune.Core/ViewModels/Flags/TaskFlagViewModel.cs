namespace Netptune.Core.ViewModels.Flags;

public sealed record TaskFlagViewModel
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public int? AutomationRuleId { get; init; }

    public DateTime CreatedAt { get; init; }
}
