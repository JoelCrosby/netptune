using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Pages;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetTaskPagesQuery(string SystemId) : IRequest<ClientResponse<List<PageBacklinkViewModel>>>;

public sealed class GetTaskPagesQueryHandler : IRequestHandler<GetTaskPagesQuery, ClientResponse<List<PageBacklinkViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IPageVisibilityService PageVisibility;

    public GetTaskPagesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IPageVisibilityService pageVisibility)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PageVisibility = pageVisibility;
    }

    public async ValueTask<ClientResponse<List<PageBacklinkViewModel>>> Handle(GetTaskPagesQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var tasks = await UnitOfWork.Tasks.GetTaskViewModels([request.SystemId], workspaceKey, cancellationToken);
        var task = tasks.FirstOrDefault();

        if (task is null)
        {
            return ClientResponse<List<PageBacklinkViewModel>>.NotFound;
        }

        var taskKey = task.Id.ToString();
        var pages = await UnitOfWork.PageLinks.GetBacklinks(PageLinkTargetType.Task, taskKey, workspaceId, cancellationToken);
        var scope = await PageVisibility.GetScope(workspaceId, cancellationToken);
        var visiblePages = pages.Where(page => scope.Allows(page.PageId)).ToList();

        return ClientResponse<List<PageBacklinkViewModel>>.Success(visiblePages);
    }
}
