using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Queries;

public sealed record GetTasksByIdQuery(IReadOnlyCollection<int> Ids) : IRequest<List<TaskViewModel>>;

public sealed class GetTasksByIdQueryHandler : IRequestHandler<GetTasksByIdQuery, List<TaskViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTasksByIdQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<TaskViewModel>> Handle(GetTasksByIdQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var tasks = await UnitOfWork.Tasks.GetTaskViewModels(request.Ids, cancellationToken);

        return tasks.Where(task => task.WorkspaceId == workspaceId).ToList();
    }
}
