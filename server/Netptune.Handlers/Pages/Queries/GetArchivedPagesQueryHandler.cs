using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetArchivedPagesQuery : IRequest<ClientResponse<List<ArchivedPageViewModel>>>;

public sealed class GetArchivedPagesQueryHandler : IRequestHandler<GetArchivedPagesQuery, ClientResponse<List<ArchivedPageViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetArchivedPagesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<ArchivedPageViewModel>>> Handle(GetArchivedPagesQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var pages = await UnitOfWork.Pages.GetArchived(workspaceId, cancellationToken);

        return ClientResponse<List<ArchivedPageViewModel>>.Success(pages);
    }
}
