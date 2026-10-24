using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Automations;

public sealed record AutomationRuleWarning
{
    public required AutomationWarningCode Code { get; init; }

    public AutomationWarningLocation Location { get; init; }

    public required string Message { get; init; }

    public int? ActionId { get; init; }

    public string? Value { get; init; }
}
