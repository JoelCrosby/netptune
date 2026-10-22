using Netptune.Core.Models.Pages;
using Netptune.Core.Services;
using Netptune.Core.Services.Pages;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Pages;

public sealed class PageVisibilityService : IPageVisibilityService
{
    private readonly IIdentityService Identity;
    private readonly INetptuneUnitOfWork UnitOfWork;

    public PageVisibilityService(IIdentityService identity, INetptuneUnitOfWork unitOfWork)
    {
        Identity = identity;
        UnitOfWork = unitOfWork;
    }

    public async Task<PublicPageScope> GetScope(int workspaceId, CancellationToken cancellationToken = default)
    {
        var isSignedIn = Identity.IsSignedIn();

        if (isSignedIn)
        {
            return PublicPageScope.Unrestricted;
        }

        var nodes = await UnitOfWork.Pages.GetVisibilityNodes(workspaceId, cancellationToken);
        var visibleIds = PublicPageVisibility.VisibleIds(nodes);

        return PublicPageScope.Restricted(visibleIds);
    }
}
