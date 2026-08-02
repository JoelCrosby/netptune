using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tags.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CreateTagChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public CreateTagChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_create_tag";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var name = AiChangePayload.ReadString(change.Payload.RootElement, "name");

        if (string.IsNullOrWhiteSpace(name))
        {
            return AiChangePayload.Failure(change, "The tag name is missing from this change.");
        }

        var request = new AddTagRequest { Tag = name };
        var response = await Mediator.Send(new CreateTagCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The tag could not be created.");
        }

        return AiChangePayload.Applied(change, response.Payload?.Id);
    }
}
