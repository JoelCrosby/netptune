using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Comments.Commands;

public sealed record DeleteCommentCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;

    public DeleteCommentCommandHandler(INetptuneUnitOfWork unitOfWork, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await UnitOfWork.Comments.GetAsync(request.Id);

        if (comment is null)
        {
            return ClientResponse.NotFound;
        }

        var commentId = comment.Id;

        await UnitOfWork.Comments.DeletePermanent(comment.Id);
        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = commentId;
            options.EntityType = EntityType.Comment;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
