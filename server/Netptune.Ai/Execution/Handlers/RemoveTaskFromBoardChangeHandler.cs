using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class RemoveTaskFromBoardChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    private readonly IMediator Mediator;

    public RemoveTaskFromBoardChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_remove_task_from_board";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var taskId = AiChangePayload.ResolveTaskId(context);
        var boardId = AiChangePayload.ReadInt(change.Payload.RootElement, "boardId");

        if (!taskId.HasValue || !boardId.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorTaskBoardChangeRefersCouldNotResolved);
        }

        var response = await Mediator.Send(
            new RemoveTaskFromBoardCommand(taskId.Value, boardId.Value),
            cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorTaskCouldNotRemovedBoard);
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
