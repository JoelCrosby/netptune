using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetPageVersionQuery(int PageId, int VersionId) : IRequest<ClientResponse<PageVersionViewModel>>;

public sealed class GetPageVersionQueryHandler : IRequestHandler<GetPageVersionQuery, ClientResponse<PageVersionViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetPageVersionQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PageVersionViewModel>> Handle(GetPageVersionQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var version = await UnitOfWork.PageVersions.GetViewModel(request.VersionId, request.PageId, workspaceId, cancellationToken);

        if (version is null)
        {
            return ClientResponse<PageVersionViewModel>.NotFound;
        }

        return ClientResponse<PageVersionViewModel>.Success(version);
    }
}
