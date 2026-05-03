using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Services.Sprints.Queries;

public sealed record GetSprintsQuery(int? ProjectId, SprintStatus? Status, int? Take) : IRequest<List<SprintViewModel>>;

public sealed class GetSprintsQueryHandler : IRequestHandler<GetSprintsQuery, List<SprintViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetSprintsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public ValueTask<List<SprintViewModel>> Handle(GetSprintsQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();

        return new(UnitOfWork.Sprints.GetSprintsAsync(workspaceKey, request.ProjectId, request.Status, request.Take, cancellationToken));
    }
}
