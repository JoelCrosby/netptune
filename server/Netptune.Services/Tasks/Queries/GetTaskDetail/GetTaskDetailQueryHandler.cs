using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Services.Tasks.Queries.GetTaskDetail;

public sealed class GetTaskDetailQueryHandler : IRequestHandler<GetTaskDetailQuery, TaskViewModel?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTaskDetailQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public ValueTask<TaskViewModel?> Handle(GetTaskDetailQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        return new ValueTask<TaskViewModel?>(UnitOfWork.Tasks.GetTaskViewModel(request.SystemId, workspaceKey));
    }
}
