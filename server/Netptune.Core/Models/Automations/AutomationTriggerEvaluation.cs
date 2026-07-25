namespace Netptune.Core.Models.Automations;

public sealed record AutomationTriggerEvaluation
{
    public bool IsMatch { get; init; }

    public bool IsEvaluable { get; init; }

    public static AutomationTriggerEvaluation NotEvaluable { get; } = new();

    public static AutomationTriggerEvaluation From(bool isMatch)
    {
        return new AutomationTriggerEvaluation
        {
            IsMatch = isMatch,
            IsEvaluable = true,
        };
    }
}
