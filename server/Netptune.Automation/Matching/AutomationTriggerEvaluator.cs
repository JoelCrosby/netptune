using Netptune.Core.Entities;
using Netptune.Core.Models.Automations;
using Netptune.Core.Services.Automations;

namespace Netptune.Automation.Matching;

internal sealed class AutomationTriggerEvaluator : IAutomationTriggerEvaluator
{
    public AutomationTriggerEvaluation Evaluate(AutomationRule rule, ProjectTask task, DateTime now)
    {
        return AutomationTriggerPredicates.Evaluate(rule, task, now);
    }
}
