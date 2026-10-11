using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Comments.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class DeleteCommentChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public DeleteCommentChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => DeleteCommentAction.Change;

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var commentId = AiChangePayload.ReadInt(change.Payload.RootElement, "commentId") ?? change.EntityId;

        if (!commentId.HasValue)
        {
            return AiChangePayload.Failure(change, "The comment this change refers to could not be resolved.");
        }

        var response = await Mediator.Send(new DeleteCommentCommand(commentId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The comment could not be deleted.");
        }

        return AiChangePayload.Applied(change, commentId.Value);
    }
}
