using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Services.ProjectTasks;

namespace Netptune.Automation.Common;

internal sealed record ActionOutcome(AutomationActionResultStatus Status, AutomationMessage? Message = null);

internal sealed record AutomationPersistenceState
{
    public required Dictionary<PlannedAutomationAction, Flag> Flags { get; init; }

    public required List<Notification> Notifications { get; init; }

    public required List<PlannedAutomationAction> AppliedTaskDeletions { get; init; }

    public required HashSet<int> DeletedTaskIds { get; init; }

    public required List<TaskMutationOutcome> TaskMutations { get; init; }
}

internal interface IActionExecutionHandler
{
    AutomationActionType Type { get; }

    Task<ActionOutcome> Execute(PlannedAutomationAction action, AutomationPersistenceState state, CancellationToken cancellationToken);
}

internal static class ActionOutcomes
{
    internal static ActionOutcome Succeeded()
    {
        return new ActionOutcome(AutomationActionResultStatus.Succeeded);
    }

    internal static ActionOutcome InvalidContribution()
    {
        return new ActionOutcome(
            AutomationActionResultStatus.Skipped,
            AutomationMessage.ActionNotExecutable);
    }
}
