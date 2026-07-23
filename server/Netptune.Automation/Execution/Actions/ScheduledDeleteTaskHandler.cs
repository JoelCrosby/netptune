using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Execution.Actions;

internal sealed class ScheduledDeleteTaskHandler : IScheduledActionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public ScheduledDeleteTaskHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public AutomationActionType Type => AutomationActionType.DeleteTask;

    public async Task<ScheduledActionOutcome> Execute(ScheduledAutomationAction action, CancellationToken cancellationToken)
    {
        if (action.OwnerId is null)
        {
            throw new InvalidOperationException("The scheduled action has no execution user.");
        }

        var affected = await UnitOfWork.Tasks.SoftDelete(action.TaskId, action.OwnerId, cancellationToken);

        if (affected == 0)
        {
            return new ScheduledActionOutcome(ScheduledAutomationActionStatus.Cancelled);
        }

        return new ScheduledActionOutcome(ScheduledAutomationActionStatus.Completed, RemoveTaskFromSearch: true);
    }
}
