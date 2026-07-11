using System.Text.Json;

using Mediator;
using Netptune.Core.Preferences;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;

namespace Netptune.Handlers.Workspaces.Queries;

public sealed record GetUserWorkspacesQuery(PageRequest? Page = null) : IRequest<List<UserWorkspaceViewModel>>;

public sealed class GetUserWorkspacesQueryHandler : IRequestHandler<GetUserWorkspacesQuery, List<UserWorkspaceViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetUserWorkspacesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<UserWorkspaceViewModel>> Handle(GetUserWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();

        var workspaces = await UnitOfWork.Workspaces.GetUserWorkspaces(userId, cancellationToken, request.Page);
        var lastVisitedSlug = await GetLastVisitedSlug(userId, cancellationToken);

        return workspaces
            .Select(workspace => new UserWorkspaceViewModel
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Description = workspace.Description,
                Slug = workspace.Slug,
                MetaInfo = workspace.MetaInfo,
                IsPublic = workspace.IsPublic,
                CreatedAt = workspace.CreatedAt,
                UpdatedAt = workspace.UpdatedAt,
                IsLastVisited = lastVisitedSlug is not null
                    && string.Equals(workspace.Slug, lastVisitedSlug, StringComparison.Ordinal),
            })
            .ToList();
    }

    private async Task<string?> GetLastVisitedSlug(string userId, CancellationToken cancellationToken)
    {
        var value = await UnitOfWork.UserPreferences.GetScopedValue(
            userId,
            PreferenceKeys.WorkspaceLastVisited,
            null,
            cancellationToken);

        if (value?.Value.RootElement.ValueKind is not JsonValueKind.String) return null;

        var slug = value.Value.RootElement.GetString();

        return string.IsNullOrWhiteSpace(slug) ? null : slug;
    }
}
