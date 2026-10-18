using Netptune.Automation.Common;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Execution.Actions;

internal sealed class ScheduledDeleteTaskHandler : IScheduledActionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventRecordWriter EventRecords;

    public ScheduledDeleteTaskHandler(INetptuneUnitOfWork unitOfWork, IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        EventRecords = eventRecords;
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

        var deletionEvent = TaskDeletionEvents.Create(action.Task, action.OwnerId);

        await EventRecords.Append(deletionEvent, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return new ScheduledActionOutcome(ScheduledAutomationActionStatus.Completed, RemoveTaskFromSearch: true);
    }
}
