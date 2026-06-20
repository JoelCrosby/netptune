using Mediator;

using Netptune.Automation.Execution;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;

namespace Netptune.JobServer.Handlers;

public sealed class AutomationTaskStatusChangedHandler : IRequestHandler<TaskStatusChangedMessage>
{
    private readonly IExecutionService AutomationExecution;

    public AutomationTaskStatusChangedHandler(IExecutionService automationExecution)
    {
        AutomationExecution = automationExecution;
    }

    public async ValueTask<Unit> Handle(TaskStatusChangedMessage request, CancellationToken cancellationToken)
    {
        await AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            EventId = request.EventId,
            WorkspaceId = request.WorkspaceId,
            TaskId = request.TaskId,
            ActorUserId = request.ActorUserId,
            OccurredAt = request.OccurredAt,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, request.OldStatusId, request.NewStatusId),
            ],
        }, cancellationToken);

        return Unit.Value;
    }
}
