using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Pages;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetPageQuery(string Slug) : IRequest<ClientResponse<PageViewModel>>;

public sealed class GetPageQueryHandler : IRequestHandler<GetPageQuery, ClientResponse<PageViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IPageVisibilityService PageVisibility;

    public GetPageQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IPageVisibilityService pageVisibility)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PageVisibility = pageVisibility;
    }

    public async ValueTask<ClientResponse<PageViewModel>> Handle(GetPageQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.TryGetCurrentUserId();
        var page = await UnitOfWork.Pages.GetViewModelBySlug(request.Slug, workspaceId, userId, cancellationToken);

        if (page is null)
        {
            return ClientResponse<PageViewModel>.NotFound;
        }

        var scope = await PageVisibility.GetScope(workspaceId, cancellationToken);
        var isHiddenFromViewer = !scope.Allows(page.Id);

        if (isHiddenFromViewer)
        {
            return ClientResponse<PageViewModel>.NotFound;
        }

        return ClientResponse<PageViewModel>.Success(page);
    }
}
