using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class MoveTaskToBoardGroupChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    private readonly IMediator Mediator;

    public MoveTaskToBoardGroupChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_move_task_to_board_group";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var taskId = AiChangePayload.ResolveTaskId(context);
        var boardGroupId = AiChangePayload.ReadInt(payload, "boardGroupId");
        var boardIdentifier = AiChangePayload.ReadString(payload, "boardIdentifier");
        var hasBoard = !string.IsNullOrWhiteSpace(boardIdentifier);

        if (!taskId.HasValue || !boardGroupId.HasValue || !hasBoard)
        {
            return AiChangePayload.Failure(change, "The task or board group this change refers to could not be resolved.");
        }

        var request = new MoveTasksToGroupRequest
        {
            BoardId = boardIdentifier!,
            TaskIds = [taskId.Value],
            NewGroupId = boardGroupId,
        };

        var response = await Mediator.Send(new MoveTasksToGroupCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The task could not be moved.");
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
