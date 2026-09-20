using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Boards.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UpdateBoardChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public UpdateBoardChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_update_board";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var boardId = AiChangePayload.ReadInt(payload, "boardId") ?? change.EntityId;

        if (!boardId.HasValue)
        {
            return AiChangePayload.Failure(change, "The board this change refers to could not be resolved.");
        }

        var request = new UpdateBoardRequest
        {
            Id = boardId,
            Name = AiChangePayload.ReadString(payload, "name"),
            Identifier = AiChangePayload.ReadString(payload, "identifier"),
        };

        var response = await Mediator.Send(new UpdateBoardCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The board could not be updated.");
        }

        return AiChangePayload.Applied(change, boardId);
    }
}
