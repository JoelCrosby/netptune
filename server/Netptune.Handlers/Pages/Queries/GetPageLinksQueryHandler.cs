using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Pages;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetPageLinksQuery(int PageId) : IRequest<ClientResponse<PageLinksViewModel>>;

public sealed class GetPageLinksQueryHandler : IRequestHandler<GetPageLinksQuery, ClientResponse<PageLinksViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IPageVisibilityService PageVisibility;

    public GetPageLinksQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IPageVisibilityService pageVisibility)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PageVisibility = pageVisibility;
    }

    public async ValueTask<ClientResponse<PageLinksViewModel>> Handle(GetPageLinksQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var page = await UnitOfWork.Pages.GetLiveInWorkspace(request.PageId, workspaceId, true, cancellationToken);

        if (page is null)
        {
            return ClientResponse<PageLinksViewModel>.NotFound;
        }

        var scope = await PageVisibility.GetScope(workspaceId, cancellationToken);
        var isHiddenFromViewer = !scope.Allows(page.Id);

        if (isHiddenFromViewer)
        {
            return ClientResponse<PageLinksViewModel>.NotFound;
        }

        var pageKey = page.Id.ToString();
        var tasks = await UnitOfWork.PageLinks.GetLinkedTasks(page.Id, workspaceId, cancellationToken);
        var pages = await UnitOfWork.PageLinks.GetLinkedPages(page.Id, workspaceId, cancellationToken);
        var backlinks = await UnitOfWork.PageLinks.GetBacklinks(PageLinkTargetType.Page, pageKey, workspaceId, cancellationToken);
        var visiblePages = pages.Where(linked => scope.Allows(linked.Id)).ToList();
        var visibleBacklinks = backlinks.Where(backlink => scope.Allows(backlink.PageId)).ToList();

        var links = new PageLinksViewModel
        {
            Tasks = tasks,
            Pages = visiblePages,
            Backlinks = visibleBacklinks,
        };

        return ClientResponse<PageLinksViewModel>.Success(links);
    }
}
