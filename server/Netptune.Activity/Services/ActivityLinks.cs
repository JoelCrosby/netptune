using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Activity.Services;

internal static class ActivityLinks
{
    public static async Task<ActivityAncestors> Resolve(
        INetptuneUnitOfWork unitOfWork,
        EntityType entityType,
        int entityId,
        CancellationToken cancellationToken)
    {
        return entityType switch
        {
            EntityType.Task => await unitOfWork.Ancestors.GetProjectTaskAncestors(entityId, cancellationToken),
            EntityType.BoardGroup => await unitOfWork.Ancestors.GetBoardGroupAncestors(entityId, cancellationToken),
            EntityType.Board => await unitOfWork.Ancestors.GetBoardAncestors(entityId, cancellationToken),
            EntityType.Project => await unitOfWork.Ancestors.GetProjectAncestors(entityId, cancellationToken),
            EntityType.Sprint => await unitOfWork.Ancestors.GetSprintAncestors(entityId, cancellationToken),
            _ => new ActivityAncestors(),
        };
    }

    public static string Build(
        string workspaceSlug,
        EntityType entityType,
        int? entityId,
        ActivityAncestors ancestors)
    {
        return entityType switch
        {
            EntityType.Task when ancestors.ProjectKey is not null => $"/{workspaceSlug}/tasks/{ancestors.ProjectKey}-{ancestors.TaskScopeId}",
            EntityType.Task => $"/{workspaceSlug}/tasks/{ancestors.TaskId}",
            EntityType.Board => $"/{workspaceSlug}/boards/{ancestors.BoardKey}",
            EntityType.Project => $"/{workspaceSlug}/projects/{entityId}",
            EntityType.Sprint => $"/{workspaceSlug}/sprints/{entityId}",
            EntityType.Status => $"/{workspaceSlug}/settings",
            _ => $"/{workspaceSlug}",
        };
    }
}
