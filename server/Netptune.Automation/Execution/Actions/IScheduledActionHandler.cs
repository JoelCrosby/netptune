using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Automation.Execution.Actions;

internal sealed record ScheduledActionOutcome(ScheduledAutomationActionStatus Status, bool RemoveTaskFromSearch = false);

internal interface IScheduledActionHandler
{
    AutomationActionType Type { get; }

    Task<ScheduledActionOutcome> Execute(ScheduledAutomationAction action, CancellationToken cancellationToken);
}
