using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;
using Netptune.Core.ViewModels.Workspace;

namespace Netptune.Services;

public class PublicWorkspaceService : IPublicWorkspaceService
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public PublicWorkspaceService(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async Task<PublicWorkspaceViewModel?> GetPublicWorkspace(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var workspace = await UnitOfWork.Workspaces.GetBySlug(
            slug,
            isReadonly: true,
            cancellationToken: cancellationToken);

        if (workspace is null || !workspace.IsPublic)
        {
            return null;
        }

        var viewModel = workspace.ToViewModel();
        var publicPermissions = NetptunePermissions.ResolvePublicPermissions(workspace.PublicPermissions);

        return new PublicWorkspaceViewModel
        {
            Id = viewModel.Id,
            Name = viewModel.Name,
            Description = viewModel.Description,
            Slug = viewModel.Slug,
            MetaInfo = viewModel.MetaInfo,
            IsPublic = viewModel.IsPublic,
            PublicPermissions = [.. publicPermissions],
        };
    }

    public async Task<PagedResponse<AssigneeViewModel>?> GetPublicWorkspaceMembers(
        string slug,
        AssigneeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var workspace = await UnitOfWork.Workspaces.GetBySlug(
            slug,
            isReadonly: true,
            cancellationToken: cancellationToken);

        if (workspace is null || !workspace.IsPublic)
        {
            return null;
        }

        var members = await UnitOfWork.Users.GetWorkspaceAssigneesPaged(workspace.Id, filter, cancellationToken);

        return new PagedResponse<AssigneeViewModel>(
            [.. members.Results],
            members.CurrentPage,
            members.PageSize,
            members.RowCount);
    }
}
