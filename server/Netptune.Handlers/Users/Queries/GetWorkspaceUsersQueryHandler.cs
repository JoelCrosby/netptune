using Mediator;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Handlers.Users.Queries;

public sealed record GetWorkspaceUsersQuery(PageRequest? Page = null) : IRequest<ClientResponse<PagedResponse<WorkspaceUserViewModel>>>;

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
        var pageRequest = request.Page ?? new PageRequest();
        var page = pageRequest.GetPage();
        var pageSize = pageRequest.GetPageSize();
        var skip = (page - 1) * pageSize;

        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, true, cancellationToken);

        if (workspace is null)
        {
            return new PagedResponse<WorkspaceUserViewModel>([], page, pageSize, 0);
        }

        var memberCount = await UnitOfWork.Users.CountWorkspaceAppUsers(workspaceKey, cancellationToken);
        var pendingCount = await UnitOfWork.WorkspaceInvites.CountPendingByWorkspaceExcludingMembers(workspace.Id, cancellationToken);
        var totalCount = memberCount + pendingCount;
        var items = new List<WorkspaceUserViewModel>(pageSize);

        if (skip < memberCount)
        {
            var workspaceAppUsers = await UnitOfWork.Users.GetWorkspaceAppUsers(workspaceKey, true, cancellationToken, pageRequest);
            items.AddRange(workspaceAppUsers.ConvertAll(user => user.ToWorkspaceViewModel()));
        }

        var remaining = pageSize - items.Count;

        if (remaining > 0)
        {
            var pendingSkip = Math.Max(0, skip - memberCount);
            var pendingInvites = await UnitOfWork.WorkspaceInvites.GetPendingByWorkspaceExcludingMembers(
                workspace.Id,
                pendingSkip,
                remaining,
                cancellationToken);

            items.AddRange(pendingInvites.Select(invite => new WorkspaceUserViewModel
            {
                Email = invite.Email,
                DisplayName = invite.Email,
                Role = WorkspaceRole.Member,
                IsPending = true,
            }));
        }

        return new PagedResponse<WorkspaceUserViewModel>(items, page, pageSize, totalCount);
    }
}
