using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Handlers.SourceControl.Queries;

public sealed record GetSourceControlIdentitiesQuery : IRequest<List<SourceControlIdentityViewModel>>;

public sealed class GetSourceControlIdentitiesQueryHandler
    : IRequestHandler<GetSourceControlIdentitiesQuery, List<SourceControlIdentityViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetSourceControlIdentitiesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<SourceControlIdentityViewModel>> Handle(
        GetSourceControlIdentitiesQuery query,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var identities = await UnitOfWork.UserSourceControlIdentities.GetForUser(userId, cancellationToken);

        var viewModels = identities.Select(SourceControlMapping.ToViewModel).ToList();

        return viewModels;
    }
}
