using Mediator;

using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.BoardGroups.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class DeleteBoardGroupChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public DeleteBoardGroupChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_delete_board_group";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var boardGroupId = AiChangePayload.ReadInt(change.Payload.RootElement, "boardGroupId") ?? change.EntityId;

        if (!boardGroupId.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorBoardGroupChangeRefersCouldNotResolved);
        }

        var response = await Mediator.Send(new DeleteBoardGroupCommand(boardGroupId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorBoardGroupCouldNotDeleted);
        }

        return AiChangePayload.Applied(change, boardGroupId);
    }
}
