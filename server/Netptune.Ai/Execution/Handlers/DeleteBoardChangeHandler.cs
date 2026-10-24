using Mediator;

using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Boards.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class DeleteBoardChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public DeleteBoardChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_delete_board";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var boardId = AiChangePayload.ReadInt(change.Payload.RootElement, "boardId") ?? change.EntityId;

        if (!boardId.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorBoardChangeRefersCouldNotResolved);
        }

        var response = await Mediator.Send(new DeleteBoardCommand(boardId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorBoardCouldNotDeleted);
        }

        return AiChangePayload.Applied(change, boardId);
    }
}
