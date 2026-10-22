using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetRecentPagesQuery : IRequest<ClientResponse<List<RecentPageViewModel>>>;

public sealed class GetRecentPagesQueryHandler : IRequestHandler<GetRecentPagesQuery, ClientResponse<List<RecentPageViewModel>>>
{
    public const int Take = 8;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetRecentPagesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<RecentPageViewModel>>> Handle(GetRecentPagesQuery request, CancellationToken cancellationToken)
    {
        var userId = Identity.TryGetCurrentUserId();

        if (userId is null)
        {
            return ClientResponse<List<RecentPageViewModel>>.Success([]);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var pages = await UnitOfWork.Pages.GetRecentlyVisited(workspaceId, userId, Take, cancellationToken);

        return ClientResponse<List<RecentPageViewModel>>.Success(pages);
    }
}
