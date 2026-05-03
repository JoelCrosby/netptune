using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Services.Sprints.Queries;

public sealed record GetSprintQuery(int Id) : IRequest<ClientResponse<SprintDetailViewModel>>;

public sealed class GetSprintQueryHandler : IRequestHandler<GetSprintQuery, ClientResponse<SprintDetailViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetSprintQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<SprintDetailViewModel>> Handle(GetSprintQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintDetailAsync(workspaceKey, request.Id, cancellationToken);

        return sprint is null
            ? ClientResponse<SprintDetailViewModel>.NotFound
            : ClientResponse<SprintDetailViewModel>.Success(sprint);
    }
}
