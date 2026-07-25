using Netptune.Core.Entities;
using Netptune.Core.Models.Automations;

namespace Netptune.Core.Services.Automations;

public interface IAutomationTriggerEvaluator
{
    AutomationTriggerEvaluation Evaluate(AutomationRule rule, ProjectTask task, DateTime now);
}
