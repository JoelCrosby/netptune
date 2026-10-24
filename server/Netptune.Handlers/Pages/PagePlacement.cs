using Netptune.Core.Entities;
using Netptune.Core.Localization;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pages;

public sealed record PagePlacementRequest(int? ParentId, int? ProjectId, int WorkspaceId);

public sealed record PagePlacementResult(Page? Parent, int? ProjectId, string? Error);

public static class PagePlacement
{
    public static string ParentMissingMessage => AppText.ErrorParentPageDoesNotExist;
    public static string ProjectMissingMessage => AppText.ErrorProjectDoesNotExist;

    public static async Task<PagePlacementResult> Resolve(
        INetptuneUnitOfWork unitOfWork,
        PagePlacementRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ParentId is { } parentId)
        {
            var parent = await unitOfWork.Pages.GetLiveInWorkspace(parentId, request.WorkspaceId, true, cancellationToken);

            return parent is null
                ? new PagePlacementResult(null, null, ParentMissingMessage)
                : new PagePlacementResult(parent, parent.ProjectId, null);
        }

        if (request.ProjectId is not { } projectId)
        {
            return new PagePlacementResult(null, null, null);
        }

        var project = await unitOfWork.Projects.GetAsync(projectId, true, cancellationToken);
        var isLiveProject = project is not null && project.WorkspaceId == request.WorkspaceId && !project.IsDeleted;

        return isLiveProject
            ? new PagePlacementResult(null, projectId, null)
            : new PagePlacementResult(null, null, ProjectMissingMessage);
    }
}
