using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tags.Commands;
using Netptune.Handlers.Tags.Queries;

namespace Netptune.Ai.Execution.Handlers;

public sealed class DeleteTagChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public DeleteTagChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => DeleteTagAction.Change;

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var tagId = AiChangePayload.ReadInt(change.Payload.RootElement, "tagId") ?? change.EntityId;

        if (!tagId.HasValue)
        {
            return AiChangePayload.Failure(change, "The tag this change refers to could not be resolved.");
        }

        var tags = await Mediator.Send(new GetTagsForWorkspaceQuery(), cancellationToken) ?? [];
        var tag = tags.FirstOrDefault(item => item.Id == tagId.Value);

        if (tag is null)
        {
            return AiChangePayload.Failure(change, "The tag no longer exists in this workspace.");
        }

        var request = new DeleteTagsRequest { Tags = [tag.Name] };
        var response = await Mediator.Send(new DeleteTagsCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The tag could not be deleted.");
        }

        return AiChangePayload.Applied(change, tag.Id);
    }
}
