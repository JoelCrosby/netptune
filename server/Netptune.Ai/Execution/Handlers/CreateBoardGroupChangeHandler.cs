using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.BoardGroups.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CreateBoardGroupChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public CreateBoardGroupChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_create_board_group";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var name = AiChangePayload.ReadString(payload, "name");
        var boardId = AiChangePayload.ReadInt(payload, "boardId")
            ?? AiChangePayload.ResolveReference(context, "boardRef");
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName || !boardId.HasValue)
        {
            return AiChangePayload.Failure(change, "The board group details are missing from this change.");
        }

        var request = new AddBoardGroupRequest
        {
            Name = name!,
            BoardId = boardId,
            StatusId = AiChangePayload.ReadInt(payload, "statusId"),
        };

        var response = await Mediator.Send(new CreateBoardGroupCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The board group could not be created.");
        }

        return AiChangePayload.Applied(change, response.Payload?.Id);
    }
}
