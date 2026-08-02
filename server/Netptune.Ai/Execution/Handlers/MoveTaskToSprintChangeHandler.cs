using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Sprints.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class MoveTaskToSprintChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    private readonly IMediator Mediator;

    public MoveTaskToSprintChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_move_task_to_sprint";

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

        var request = new AddTasksToSprintRequest { TaskIds = [taskId.Value] };
        var response = await Mediator.Send(
            new AddTasksToSprintCommand(sprintId.Value, request),
            cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The task could not be moved into the sprint.");
        }

        return AiChangePayload.Applied(change, taskId);
    }

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Tasks.Update,
    };

    public Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        return AiTaskUndo.Capture(Mediator, AiChangePayload.ResolveTaskId(context), cancellationToken);
    }

    public Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        return AiTaskUndo.Restore(Mediator, context, cancellationToken);
    }
}
