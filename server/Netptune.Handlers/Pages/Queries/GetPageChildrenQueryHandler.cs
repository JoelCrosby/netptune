using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetPageChildrenQuery(int PageId) : IRequest<ClientResponse<List<PageChildViewModel>>>;

public sealed class GetPageChildrenQueryHandler : IRequestHandler<GetPageChildrenQuery, ClientResponse<List<PageChildViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetPageChildrenQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<PageChildViewModel>>> Handle(GetPageChildrenQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var existingIds = await UnitOfWork.Pages.GetExistingIds([request.PageId], workspaceId, cancellationToken);

        if (existingIds.Count == 0)
        {
            return ClientResponse<List<PageChildViewModel>>.NotFound;
        }

        var children = await UnitOfWork.Pages.GetChildren(request.PageId, workspaceId, cancellationToken);

        return ClientResponse<List<PageChildViewModel>>.Success(children);
    }
}
