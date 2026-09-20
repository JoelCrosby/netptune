using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.BoardGroups.Commands;
using Netptune.Handlers.BoardGroups.Queries;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UpdateBoardGroupChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public UpdateBoardGroupChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_update_board_group";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var boardGroupId = AiChangePayload.ReadInt(payload, "boardGroupId") ?? change.EntityId;

        if (!boardGroupId.HasValue)
        {
            return AiChangePayload.Failure(change, "The board group this change refers to could not be resolved.");
        }

        // UpdateBoardGroupCommand takes the group by id without a workspace check of its own, so the
        // caller has to make one — nothing outside an endpoint policy is standing in front of it here.
        var group = await Mediator.Send(new GetBoardGroupQuery(boardGroupId.Value), cancellationToken);

        if (group is null)
        {
            return AiChangePayload.Failure(change, "The board group is no longer in this workspace.");
        }

        var request = new UpdateBoardGroupRequest
        {
            BoardGroupId = group.Id,
            Name = AiChangePayload.ReadString(payload, "name"),
            StatusId = AiChangePayload.ReadInt(payload, "statusId"),
            ClearStatus = AiChangePayload.ReadBool(payload, "clearStatus") ?? false,
        };

        var response = await Mediator.Send(new UpdateBoardGroupCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The board group could not be updated.");
        }

        return AiChangePayload.Applied(change, group.Id);
    }
}
