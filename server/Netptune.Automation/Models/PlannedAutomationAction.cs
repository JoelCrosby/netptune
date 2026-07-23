using Netptune.Core.Entities;
using Netptune.Core.Models.Automations;

namespace Netptune.Automation.Models;

internal sealed class PlannedAutomationAction
{
    public required PendingAutomationExecution Execution { get; init; }

    public required AutomationAction Action { get; init; }

    public required AutomationActionPlanContribution Contribution { get; init; }

    public required AutomationActionResult Result { get; init; }
}
