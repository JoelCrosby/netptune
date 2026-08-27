using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Statuses;

namespace Netptune.Handlers.Statuses.Queries;

public sealed record GetStatusesPageQuery(StatusPageFilter Filter) : IRequest<ClientResponse<PagedResponse<StatusViewModel>>>;

public sealed class GetStatusesPageQueryHandler : IRequestHandler<GetStatusesPageQuery, ClientResponse<PagedResponse<StatusViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetStatusesPageQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<StatusViewModel>>> Handle(GetStatusesPageQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null)
        {
            return ClientResponse<PagedResponse<StatusViewModel>>.NotFound;
        }

        var page = await UnitOfWork.Statuses.GetPageForWorkspace(workspaceId.Value, request.Filter, cancellationToken);

        return ClientResponse<PagedResponse<StatusViewModel>>.Success(page);
    }
}
