using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Relations;

namespace Netptune.Handlers.Relations.Queries;

public sealed record GetTaskRelationsQuery(string SystemId) : IRequest<List<TaskRelationViewModel>?>;

public sealed class GetTaskRelationsQueryHandler : IRequestHandler<GetTaskRelationsQuery, List<TaskRelationViewModel>?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTaskRelationsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<TaskRelationViewModel>?> Handle(GetTaskRelationsQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return null;

        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, workspaceKey, cancellationToken);

        if (taskId is null) return null;

        return await UnitOfWork.ProjectTaskRelations.GetRelationsForTask(taskId.Value, workspaceId.Value, cancellationToken);
    }
}
