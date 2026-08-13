using Mediator;

using Netptune.Core.Constants;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Handlers.Comments.Commands;

public sealed record AddCommentReactionCommand(int CommentId, CommentReactionRequest Request) : IRequest<ClientResponse<CommentViewModel>>;

public sealed class AddCommentReactionCommandHandler : IRequestHandler<AddCommentReactionCommand, ClientResponse<CommentViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public AddCommentReactionCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<CommentViewModel>> Handle(AddCommentReactionCommand request, CancellationToken cancellationToken)
    {
        var value = ReactionValues.Normalize(request.Request.Value);

        if (value is null)
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
        var hasReacted = await UnitOfWork.Reactions.HasUserReaction(comment.Id, userId, value, cancellationToken);

        if (!hasReacted)
        {
            var reaction = new Reaction
            {
                CommentId = comment.Id,
                Value = value,
                OwnerId = userId,
                WorkspaceId = comment.WorkspaceId,
            };

            await UnitOfWork.Reactions.AddAsync(reaction, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        }

        var result = await UnitOfWork.Comments.GetCommentViewModel(comment.Id, cancellationToken);

        if (result is null)
        {
            return ClientResponse<CommentViewModel>.Failed("add reaction failed");
        }

        return result;
    }
}
