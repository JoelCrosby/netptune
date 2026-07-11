using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Handlers.Sprints.Queries;

public sealed record GetCurrentSprintQuery : IRequest<ClientResponse<SprintDetailViewModel?>>;

public sealed class GetCurrentSprintQueryHandler : IRequestHandler<GetCurrentSprintQuery, ClientResponse<SprintDetailViewModel?>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetCurrentSprintQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<SprintDetailViewModel?>> Handle(GetCurrentSprintQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var userId = Identity.GetCurrentUserId();

        var sprint = await UnitOfWork.Sprints.GetCurrentSprintForUserAsync(workspaceKey, userId, cancellationToken);

        return ClientResponse<SprintDetailViewModel?>.Success(sprint);
    }
}
