using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Services.Comments.Queries.GetCommentsForTask;

public sealed class GetCommentsForTaskQueryHandler : IRequestHandler<GetCommentsForTaskQuery, List<CommentViewModel>?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetCommentsForTaskQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<CommentViewModel>?> Handle(GetCommentsForTaskQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, workspaceKey);

        if (taskId is null)
        {
            return null;
        }

        return await UnitOfWork.Comments.GetCommentViewModelsForTask(taskId.Value);
    }
}
