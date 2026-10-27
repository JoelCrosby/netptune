using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
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
        var boardGroupId = AiChangePayload.ReadInt(payload, "boardGroupId")
            ?? AiChangePayload.ResolveReference(context, "boardGroupRef");

        if (!taskId.HasValue || !boardGroupId.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorTaskBoardGroupChangeRefersCouldNotResolved);
        }

        var command = new MoveTasksToBoardGroupCommand([taskId.Value], boardGroupId.Value);
        var response = await Mediator.Send(command, cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorTaskCouldNotMoved);
        }

        return AiChangePayload.Applied(change, taskId);
    }

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Tasks.Move,
    };

    public Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        return AiTaskUndo.Capture(Mediator, AiChangePayload.ResolveTaskId(context), cancellationToken);
    }

    public Task<JsonDocument?> CaptureApplied(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        return AiTaskUndo.CaptureApplied(Mediator, context, cancellationToken);
    }

    public Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        return AiTaskUndo.Restore(Mediator, context, cancellationToken);
    }
}
