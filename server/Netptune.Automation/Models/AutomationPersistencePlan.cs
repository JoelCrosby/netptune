using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Automation.Models;

internal sealed record AutomationPersistencePlan
{
    public required AutomationTriggerType TriggerType { get; init; }

    public required List<AutomationRun> Runs { get; init; }

    public required List<PlannedAutomationAction> Actions { get; init; }

    public required Dictionary<PlannedAutomationAction, Flag> Flags { get; init; }
}
