using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Handlers.Comments.Queries;

public sealed record GetTaskCommentQuery(string SystemId, int CommentId) : IRequest<CommentViewModel?>;

public sealed class GetTaskCommentQueryHandler : IRequestHandler<GetTaskCommentQuery, CommentViewModel?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTaskCommentQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<CommentViewModel?> Handle(GetTaskCommentQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, workspaceKey, cancellationToken);

        if (taskId is null)
        {
            return null;
        }

        var comment = await UnitOfWork.Comments.GetTaskCommentViewModel(taskId.Value, request.CommentId, cancellationToken);

        return comment;
    }
}
