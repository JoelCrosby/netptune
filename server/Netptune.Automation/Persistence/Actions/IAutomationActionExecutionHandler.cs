using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Services.ProjectTasks;

namespace Netptune.Automation.Persistence.Actions;

internal sealed record AutomationActionExecutionOutcome(AutomationActionResultStatus Status, string? Message = null);

internal sealed record AutomationPersistenceState
{
    public required Dictionary<PlannedAutomationAction, Flag> Flags { get; init; }

    public required List<Notification> Notifications { get; init; }

    public required List<PlannedAutomationAction> AppliedTaskDeletions { get; init; }

    public required HashSet<int> DeletedTaskIds { get; init; }

    public required List<TaskMutationOutcome> TaskMutations { get; init; }
}

internal interface IAutomationActionExecutionHandler
{
    AutomationActionType Type { get; }

    Task<AutomationActionExecutionOutcome> Execute(PlannedAutomationAction action, AutomationPersistenceState state, CancellationToken cancellationToken);
}

internal static class AutomationActionExecutionOutcomes
{
    internal static AutomationActionExecutionOutcome Succeeded()
    {
        return new AutomationActionExecutionOutcome(AutomationActionResultStatus.Succeeded);
    }

    internal static AutomationActionExecutionOutcome InvalidContribution()
    {
        return new AutomationActionExecutionOutcome(
            AutomationActionResultStatus.Skipped,
            "The action did not produce an executable contribution.");
    }
}
