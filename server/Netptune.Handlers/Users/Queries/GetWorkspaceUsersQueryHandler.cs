using Mediator;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Handlers.Users.Queries;

public sealed record GetWorkspaceUsersQuery(PageRequest? Page = null) : IRequest<List<WorkspaceUserViewModel>>;

public sealed class GetWorkspaceUsersQueryHandler : IRequestHandler<GetWorkspaceUsersQuery, List<WorkspaceUserViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetWorkspaceUsersQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<WorkspaceUserViewModel>> Handle(GetWorkspaceUsersQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceAppUsers = await UnitOfWork.Users.GetWorkspaceAppUsers(workspaceKey, true, cancellationToken, request.Page);

        var results = workspaceAppUsers.ConvertAll(user => user.ToWorkspaceViewModel());

        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, true, cancellationToken);

        if (workspace is not null)
        {
            var pendingInvites = await UnitOfWork.WorkspaceInvites.GetPendingByWorkspace(workspace.Id, cancellationToken);

            var existingEmails = results.Select(u => u.Email?.ToUpperInvariant()).ToHashSet();

            var pendingViewModels = pendingInvites
                .Where(invite => !existingEmails.Contains(invite.Email.ToUpperInvariant()))
                .Select(invite => new WorkspaceUserViewModel
                {
                    Email = invite.Email,
                    DisplayName = invite.Email,
                    Role = WorkspaceRole.Member,
                    IsPending = true,
                });

            results.AddRange(pendingViewModels);
        }

        return results;
    }
}
