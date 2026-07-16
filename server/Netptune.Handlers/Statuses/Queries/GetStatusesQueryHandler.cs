using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Statuses;

namespace Netptune.Handlers.Statuses.Queries;

public sealed record GetStatusesQuery(StatusFilter Filter) : IRequest<List<StatusViewModel>?>;

public sealed class GetStatusesQueryHandler : IRequestHandler<GetStatusesQuery, List<StatusViewModel>?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetStatusesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<StatusViewModel>?> Handle(GetStatusesQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return null;

        return await UnitOfWork.Statuses.GetViewModelsForWorkspace(workspaceId.Value, request.Filter.EntityType, cancellationToken);
    }
}
