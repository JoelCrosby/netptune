using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record ActionPlan
{
    public required List<AutomationRun> Runs { get; init; }

    public required List<PlannedAutomationAction> Actions { get; init; }
}
