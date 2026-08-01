using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Sprints.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class AddTasksToSprintChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public AddTasksToSprintChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_add_tasks_to_sprint";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var sprintId = AiChangePayload.ReadInt(payload, "sprintId") ?? change.EntityId;
        var taskIds = AiChangePayload.ReadIntArray(payload, "taskIds");

        if (!sprintId.HasValue || taskIds.Count == 0)
        {
            return AiChangePayload.Failure(change, "The sprint or tasks this change refers to could not be resolved.");
        }

        var request = new AddTasksToSprintRequest { TaskIds = taskIds };
        var response = await Mediator.Send(new AddTasksToSprintCommand(sprintId.Value, request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The tasks could not be added to the sprint.");
        }

        return AiChangePayload.Applied(change, sprintId);
    }
}
