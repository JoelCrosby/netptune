using Netptune.Core.Enums;
using Netptune.Core.Requests;

namespace Netptune.Core.Models.Automations;

public class AutomationRuleFilter : PageRequest
{
    public string? Search { get; set; }

    public bool? IsEnabled { get; set; }

    public string? TriggerTypes { get; set; }

    public IReadOnlyList<AutomationTriggerType> GetTriggerTypes()
    {
        if (string.IsNullOrWhiteSpace(TriggerTypes))
        {
            return [];
        }

        return TriggerTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Enum.TryParse<AutomationTriggerType>(value, true, out var triggerType)
                ? triggerType
                : (AutomationTriggerType?)null)
            .Where(triggerType => triggerType.HasValue)
            .Select(triggerType => triggerType!.Value)
            .Distinct()
            .ToList();
    }
}
