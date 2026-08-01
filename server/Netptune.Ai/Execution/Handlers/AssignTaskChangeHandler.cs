using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class AssignTaskChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public AssignTaskChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_assign_task";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var taskId = AiChangePayload.ResolveTaskId(context);

        if (!taskId.HasValue)
        {
            return AiChangePayload.Failure(change, "The task this change refers to could not be resolved.");
        }

        var payload = change.Payload.RootElement;
        var request = new UpdateProjectTaskRequest
        {
            Id = taskId.Value,
            AssigneeIds = AiChangePayload.ReadStringArray(payload, "assigneeIds"),
        };

        var response = await Mediator.Send(new UpdateTaskCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The task could not be reassigned.");
        }

        return AiChangePayload.Applied(change, taskId);
    }
}
