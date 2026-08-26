using System.Text.Json;

using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Models.Workspaces;
using Netptune.Core.Preferences;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;

namespace Netptune.Handlers.Workspaces.Queries;

public sealed record GetUserWorkspacesQuery(PageRequest? Page = null) : IRequest<List<UserWorkspaceViewModel>>;

public sealed class GetUserWorkspacesQueryHandler : IRequestHandler<GetUserWorkspacesQuery, List<UserWorkspaceViewModel>>
{
    private const int MaxMemberSample = 5;

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
        var members = await GetMemberSummaries(workspaces, cancellationToken);

        return workspaces
            .Select(workspace =>
            {
                var summary = members.GetValueOrDefault(workspace.Id);

                return new UserWorkspaceViewModel
                {
                    Id = workspace.Id,
                    Name = workspace.Name,
                    Description = workspace.Description,
                    Slug = workspace.Slug,
                    MetaInfo = workspace.MetaInfo,
                    IsPublic = workspace.IsPublic,
                    AssistantEnabled = workspace.AssistantEnabled,
                    AllowAssistantDataSampling = workspace.AllowAssistantDataSampling,
                    MaxUploadBytes = workspace.MaxUploadBytes,
                    CreatedAt = workspace.CreatedAt,
                    UpdatedAt = workspace.UpdatedAt,
                    IsLastVisited = lastVisitedSlug is not null
                        && string.Equals(workspace.Slug, lastVisitedSlug, StringComparison.Ordinal),
                    Members = summary?.Members ?? [],
                    MemberCount = summary?.MemberCount ?? 0,
                };
            })
            .ToList();
    }

    private async Task<Dictionary<int, WorkspaceMemberSummary>> GetMemberSummaries(
        List<Workspace> workspaces,
        CancellationToken cancellationToken)
    {
        if (workspaces.Count == 0) return [];

        var ids = workspaces.Select(workspace => workspace.Id).ToList();

        return await UnitOfWork.Workspaces.GetMemberSummaries(ids, MaxMemberSample, cancellationToken);
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
