using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Handlers.Sprints.Queries;

public sealed record GetSprintByIdentifierQuery(string Identifier) : IRequest<ClientResponse<SprintDetailViewModel>>;

public sealed class GetSprintByIdentifierQueryHandler : IRequestHandler<GetSprintByIdentifierQuery, ClientResponse<SprintDetailViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetSprintByIdentifierQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<SprintDetailViewModel>> Handle(GetSprintByIdentifierQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintDetailAsync(workspaceKey, request.Identifier, cancellationToken);

        return sprint is null
            ? ClientResponse<SprintDetailViewModel>.NotFound
            : ClientResponse<SprintDetailViewModel>.Success(sprint);
    }
}
