using Mediator;

using Netptune.Core.ViewModels.Comments;
using Netptune.Handlers.Comments.Queries;

namespace Netptune.Ai.Tools;

public static class AiCommentLookup
{
    public const string EntityType = "comment";

    public static async Task<CommentViewModel?> Find(
        IMediator mediator,
        string systemId,
        int commentId,
        CancellationToken cancellationToken)
    {
        var comment = await mediator.Send(new GetTaskCommentQuery(systemId, commentId), cancellationToken);

        return comment;
    }
}
