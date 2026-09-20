using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.BoardGroups.Commands;
using Netptune.Handlers.BoardGroups.Queries;

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
            return AiChangePayload.Failure(change, "The group order is missing from this change.");
        }

        // UpdateBoardGroupCommand takes the group by id without a workspace check of its own, so the
        // order is matched against the groups this workspace actually has on the board.
        var options = await Mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);
        var boardGroups = options.Where(option => option.BoardId == boardId!.Value);
        var groupsOnBoard = boardGroups.Select(option => option.Id).ToHashSet();
        var coversEveryGroup = groupIds.Distinct().Count() == groupIds.Count
            && groupIds.Count == groupsOnBoard.Count
            && groupIds.All(groupsOnBoard.Contains);

        if (!coversEveryGroup)
        {
            return AiChangePayload.Failure(
                change,
                "The groups on this board have changed since the order was proposed.");
        }

        var position = 0;

        foreach (var groupId in groupIds)
        {
            position++;

            var request = new UpdateBoardGroupRequest
            {
                BoardGroupId = groupId,
                SortOrder = position,
            };

            var response = await Mediator.Send(new UpdateBoardGroupCommand(request), cancellationToken);

            if (!response.IsSuccess)
            {
                return AiChangePayload.Failure(change, response.Message ?? "The groups could not be reordered.");
            }
        }

        return AiChangePayload.Applied(change, boardId);
    }
}
