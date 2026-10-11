using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetPageLinksQuery(int PageId) : IRequest<ClientResponse<PageLinksViewModel>>;

public sealed class GetPageLinksQueryHandler : IRequestHandler<GetPageLinksQuery, ClientResponse<PageLinksViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetPageLinksQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PageLinksViewModel>> Handle(GetPageLinksQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var page = await UnitOfWork.Pages.GetLiveInWorkspace(request.PageId, workspaceId, true, cancellationToken);

        if (page is null)
        {
            return ClientResponse<PageLinksViewModel>.NotFound;
        }

        var pageKey = page.Id.ToString();
        var tasks = await UnitOfWork.PageLinks.GetLinkedTasks(page.Id, workspaceId, cancellationToken);
        var pages = await UnitOfWork.PageLinks.GetLinkedPages(page.Id, workspaceId, cancellationToken);
        var backlinks = await UnitOfWork.PageLinks.GetBacklinks(PageLinkTargetType.Page, pageKey, workspaceId, cancellationToken);

        var links = new PageLinksViewModel
        {
            Tasks = tasks,
            Pages = pages,
            Backlinks = backlinks,
        };

        return ClientResponse<PageLinksViewModel>.Success(links);
    }
}
