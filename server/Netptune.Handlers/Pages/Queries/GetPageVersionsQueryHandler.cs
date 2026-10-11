using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetPageVersionsQuery(int PageId) : IRequest<ClientResponse<List<PageVersionSummaryViewModel>>>;

public sealed class GetPageVersionsQueryHandler : IRequestHandler<GetPageVersionsQuery, ClientResponse<List<PageVersionSummaryViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetPageVersionsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<PageVersionSummaryViewModel>>> Handle(GetPageVersionsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var versions = await UnitOfWork.PageVersions.GetSummaries(request.PageId, workspaceId, cancellationToken);

        return ClientResponse<List<PageVersionSummaryViewModel>>.Success(versions);
    }
}
