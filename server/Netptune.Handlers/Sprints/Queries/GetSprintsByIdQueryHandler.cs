using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Handlers.Sprints.Queries;

public sealed record GetSprintsByIdQuery(IReadOnlyCollection<int> Ids) : IRequest<List<SprintViewModel>>;

public sealed class GetSprintsByIdQueryHandler : IRequestHandler<GetSprintsByIdQuery, List<SprintViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetSprintsByIdQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<SprintViewModel>> Handle(GetSprintsByIdQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var sprints = await UnitOfWork.Sprints.GetSprintViewModels(request.Ids, cancellationToken);

        return sprints.Where(sprint => sprint.WorkspaceId == workspaceId).ToList();
    }
}
