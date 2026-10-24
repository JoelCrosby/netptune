using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Localization;
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
            return AiChangePayload.Failure(change, AppText.AiChangeErrorCommentChangeRefersCouldNotResolved);
        }

        var response = await Mediator.Send(new DeleteCommentCommand(commentId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorCommentCouldNotDeleted);
        }

        return AiChangePayload.Applied(change, commentId.Value);
    }
}
