using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Sprints.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class RemoveTaskFromSprintChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public RemoveTaskFromSprintChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_remove_task_from_sprint";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var taskId = AiChangePayload.ResolveTaskId(context);
        var sprintId = AiChangePayload.ReadInt(change.Payload.RootElement, "sprintId");

        if (!taskId.HasValue || !sprintId.HasValue)
        {
            return AiChangePayload.Failure(change, "The task or sprint this change refers to could not be resolved.");
        }

        var response = await Mediator.Send(
            new RemoveTaskFromSprintCommand(sprintId.Value, taskId.Value),
            cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The task could not be removed from the sprint.");
        }

        return AiChangePayload.Applied(change, taskId);
    }
}
