using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Boards.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CreateBoardChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public CreateBoardChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_create_board";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var name = AiChangePayload.ReadString(payload, "name");
        var identifier = AiChangePayload.ReadString(payload, "identifier");
        var projectId = AiChangePayload.ReadInt(payload, "projectId");
        var hasName = !string.IsNullOrWhiteSpace(name);
        var hasIdentifier = !string.IsNullOrWhiteSpace(identifier);

        if (!hasName || !hasIdentifier || !projectId.HasValue)
        {
            return AiChangePayload.Failure(change, "The board details are missing from this change.");
        }

        var request = new AddBoardRequest
        {
            Name = name!,
            Identifier = identifier!,
            ProjectId = projectId,
        };

        var response = await Mediator.Send(new CreateBoardCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The board could not be created.");
        }

        return AiChangePayload.Applied(change, response.Payload?.Id);
    }
}
