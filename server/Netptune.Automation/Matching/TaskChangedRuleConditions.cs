using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Automations;

namespace Netptune.Automation.Matching;

internal static class TaskChangedRuleConditions
{
    public static bool Match(AutomationRule rule, TaskChangedMessage message, ProjectTask task)
    {
        var isTaskChangedRule = rule.TriggerType == AutomationTriggerType.TaskChanged;

        if (!isTaskChangedRule)
        {
            return false;
        }

        var isInScope = AutomationRuleScope.Contains(rule, task);

        if (!isInScope)
        {
            return false;
        }

        var configuredFields = JsonUtils.ReadEnumList<TaskChangeField>(rule.TriggerConfig, "fields");
        var watchesAllFields = configuredFields.Count == 0;
        var allTaskFields = Enum.GetValues<TaskChangeField>().ToHashSet();
        var configuredFieldSet = configuredFields.ToHashSet();
        var watchedFields = watchesAllFields
            ? allTaskFields
            : configuredFieldSet;

        var matchingChanges = message.Changes
            .Where(change => watchedFields.Contains(change.Field))
            .ToList();

        if (matchingChanges.Count == 0)
        {
            return false;
        }

        var conditionGroup = JsonUtils.ReadObject<AutomationConditionGroup>(rule.TriggerConfig, "conditionGroup");

        if (conditionGroup is null)
        {
            return true;
        }

        return conditionGroup.Matches(task, message);
    }
}
