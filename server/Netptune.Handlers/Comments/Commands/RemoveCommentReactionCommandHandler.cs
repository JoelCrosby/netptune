using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Handlers.Comments.Commands;

public sealed record RemoveCommentReactionCommand(int CommentId, string Value) : IRequest<ClientResponse<CommentViewModel>>;

public sealed class RemoveCommentReactionCommandHandler : IRequestHandler<RemoveCommentReactionCommand, ClientResponse<CommentViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public RemoveCommentReactionCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<CommentViewModel>> Handle(RemoveCommentReactionCommand request, CancellationToken cancellationToken)
    {
        var value = request.Value?.Trim();

        if (string.IsNullOrEmpty(value))
        {
            return ClientResponse<CommentViewModel>.Failed("Reaction is not supported.");
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var comment = await UnitOfWork.Comments.GetCommentForUpdate(request.CommentId, workspaceId, cancellationToken);

        if (comment is null)
        {
            return ClientResponse<CommentViewModel>.NotFound;
        }

        var userId = Identity.GetCurrentUserId();

        await UnitOfWork.Reactions.DeleteUserReaction(comment.Id, userId, value, cancellationToken);

        var result = await UnitOfWork.Comments.GetCommentViewModel(comment.Id, cancellationToken);

        if (result is null)
        {
            return ClientResponse<CommentViewModel>.Failed("remove reaction failed");
        }

        return result;
    }
}
