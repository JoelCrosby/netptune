using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetPageQuery(string Slug) : IRequest<ClientResponse<PageViewModel>>;

public sealed class GetPageQueryHandler : IRequestHandler<GetPageQuery, ClientResponse<PageViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetPageQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PageViewModel>> Handle(GetPageQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var page = await UnitOfWork.Pages.GetViewModelBySlug(request.Slug, workspaceId, userId, cancellationToken);

        if (page is null)
        {
            return ClientResponse<PageViewModel>.NotFound;
        }

        return ClientResponse<PageViewModel>.Success(page);
    }
}
