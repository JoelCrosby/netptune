using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Services.Tasks.Queries;

public sealed record GetTasksQuery(TaskFilter? Filter = null) : IRequest<List<TaskViewModel>>;

public sealed class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, List<TaskViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTasksQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public ValueTask<List<TaskViewModel>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        return new ValueTask<List<TaskViewModel>>(UnitOfWork.Tasks.GetTasksAsync(workspaceKey, request.Filter, true, cancellationToken));
    }
}
