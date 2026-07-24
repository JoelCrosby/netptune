using Netptune.Core.Enums;

namespace Netptune.Core.Models.Automations;

public sealed record AutomationConditionExplanation
{
    public TaskChangeField Field { get; init; }

    public AutomationConditionOperator Operator { get; init; }

    public string? Value { get; init; }

    public string? ActualValue { get; init; }

    public bool IsMatch { get; init; }

    public bool IsEvaluable { get; init; }
}

public sealed record AutomationConditionGroupExplanation
{
    public AutomationConditionGroupOperator Operator { get; init; }

    public bool IsMatch { get; init; }

    public List<AutomationConditionExplanation> Conditions { get; init; } = [];

    public List<AutomationConditionGroupExplanation> Groups { get; init; } = [];
}
