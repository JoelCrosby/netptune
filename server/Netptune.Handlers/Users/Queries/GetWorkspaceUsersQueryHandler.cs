using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Handlers.Users.Queries;

public sealed record GetWorkspaceUsersQuery(WorkspaceUserFilter? Filter = null) : IRequest<ClientResponse<PagedResponse<WorkspaceUserViewModel>>>;

public sealed class GetWorkspaceUsersQueryHandler : IRequestHandler<GetWorkspaceUsersQuery, ClientResponse<PagedResponse<WorkspaceUserViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetWorkspaceUsersQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<WorkspaceUserViewModel>>> Handle(GetWorkspaceUsersQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var filter = request.Filter ?? new WorkspaceUserFilter();
        var pagination = filter.GetPagination();

        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, true, cancellationToken);

        if (workspace is null)
        {
            return new PagedResponse<WorkspaceUserViewModel>([], pagination.Page, pagination.PageSize, 0);
        }

        // Members and pending invites are merged, sorted and paginated in the database.
        var result = await UnitOfWork.Users.GetWorkspaceUsersPaged(workspace.Id, filter, cancellationToken);

        return new PagedResponse<WorkspaceUserViewModel>(
            [.. result.Results],
            result.CurrentPage,
            result.PageSize,
            result.RowCount);
    }
}
