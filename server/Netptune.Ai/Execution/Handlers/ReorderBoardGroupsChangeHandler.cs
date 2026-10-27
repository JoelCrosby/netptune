using Mediator;

using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.BoardGroups.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class ReorderBoardGroupsChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public ReorderBoardGroupsChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_reorder_board_groups";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var boardId = AiChangePayload.ReadInt(payload, "boardId") ?? change.EntityId;
        var groupIds = AiChangePayload.ReadIntArray(payload, "groupIds");
        var hasOrder = boardId.HasValue && groupIds.Count > 0;

        if (!hasOrder)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorGroupOrderMissingChange);
        }

        var response = await Mediator.Send(new ReorderBoardGroupsCommand(boardId!.Value, groupIds), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorGroupsCouldNotReordered);
        }

        return AiChangePayload.Applied(change, boardId);
    }
}
