using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ServiceAccounts;

namespace Netptune.Handlers.ServiceAccounts.Queries;

public sealed record GetServiceAccountsQuery : IRequest<List<ServiceAccountViewModel>>;

public sealed class GetServiceAccountsQueryHandler : IRequestHandler<GetServiceAccountsQuery, List<ServiceAccountViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetServiceAccountsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<ServiceAccountViewModel>> Handle(
        GetServiceAccountsQuery query,
        CancellationToken cancellationToken)
    {
        return await UnitOfWork.ServiceAccounts.GetInWorkspace(await Identity.GetWorkspaceId(), cancellationToken);
    }
}
