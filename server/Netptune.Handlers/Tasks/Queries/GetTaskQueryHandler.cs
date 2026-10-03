using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Queries;

public sealed record GetTaskQuery(int Id) : IRequest<TaskViewModel?>;

public sealed class GetTaskQueryHandler : IRequestHandler<GetTaskQuery, TaskViewModel?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTaskQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<TaskViewModel?> Handle(GetTaskQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var task = await UnitOfWork.Tasks.GetTaskViewModel(request.Id, cancellationToken);
        var isInWorkspace = task?.WorkspaceId == workspaceId;

        return isInWorkspace ? task : null;
    }
}
