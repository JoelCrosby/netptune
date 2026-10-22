using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Pages;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetPageChildrenQuery(int PageId) : IRequest<ClientResponse<List<PageChildViewModel>>>;

public sealed class GetPageChildrenQueryHandler : IRequestHandler<GetPageChildrenQuery, ClientResponse<List<PageChildViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IPageVisibilityService PageVisibility;

    public GetPageChildrenQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IPageVisibilityService pageVisibility)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PageVisibility = pageVisibility;
    }

    public async ValueTask<ClientResponse<List<PageChildViewModel>>> Handle(GetPageChildrenQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var existingIds = await UnitOfWork.Pages.GetExistingIds([request.PageId], workspaceId, cancellationToken);
        var scope = await PageVisibility.GetScope(workspaceId, cancellationToken);
        var isMissing = existingIds.Count == 0;
        var isHiddenFromViewer = !scope.Allows(request.PageId);

        if (isMissing || isHiddenFromViewer)
        {
            return ClientResponse<List<PageChildViewModel>>.NotFound;
        }

        var children = await UnitOfWork.Pages.GetChildren(request.PageId, workspaceId, cancellationToken);
        var visibleChildren = children.Where(child => scope.Allows(child.Id)).ToList();

        return ClientResponse<List<PageChildViewModel>>.Success(visibleChildren);
    }
}
