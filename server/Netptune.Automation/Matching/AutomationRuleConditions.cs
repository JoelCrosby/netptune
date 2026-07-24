using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Models.Automations;

namespace Netptune.Automation.Matching;

internal static class AutomationRuleConditions
{
    public static bool Match(AutomationRule rule, ProjectTask task)
    {
        var conditionGroup = JsonUtils.ReadObject<AutomationConditionGroup>(rule.TriggerConfig, "conditionGroup");

        return conditionGroup?.Matches(task) ?? true;
    }
}
