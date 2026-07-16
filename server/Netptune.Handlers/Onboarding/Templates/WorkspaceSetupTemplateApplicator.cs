using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Onboarding.Templates;
using Netptune.Core.Tags;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Onboarding.Templates;

internal static class WorkspaceSetupTemplateApplicator
{
    /// <summary>
    /// Non-destructively merges the template's missing statuses, tags, and relation types
    /// into a workspace before its board groups are resolved.
    /// </summary>
    internal static async Task MergeWorkspaceDefaultsAsync(
        WorkspaceSetupTemplateDefinition template,
        int workspaceId,
        string ownerId,
        INetptuneUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        await MergeStatusesAsync(
            template.Statuses,
            workspaceId,
            ownerId,
            unitOfWork,
            cancellationToken);

        await MergeRelationTypesAsync(
            template.RelationTypes,
            workspaceId,
            ownerId,
            unitOfWork,
            cancellationToken);

        await MergeTagsAsync(
            template.Tags,
            workspaceId,
            ownerId,
            unitOfWork,
            cancellationToken);

        await unitOfWork.CompleteAsync(cancellationToken);
    }

    private static async Task MergeStatusesAsync(
        IReadOnlyList<SetupTemplateStatusDefinition> definitions,
        int workspaceId,
        string ownerId,
        INetptuneUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var existingStatuses = await unitOfWork.Statuses.GetAllInWorkspace(
            workspaceId,
            includeDeleted: false,
            cancellationToken: cancellationToken);

        var taskStatuses = existingStatuses
            .Where(status => status.EntityType == EntityType.Task)
            .ToList();

        var existingKeys = taskStatuses
            .Select(status => status.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingStatuses = definitions
            .Where(status => !existingKeys.Contains(status.Key))
            .Select((status, index) => new Status
            {
                WorkspaceId = workspaceId,
                OwnerId = ownerId,
                EntityType = EntityType.Task,
                Name = status.Name,
                Key = status.Key,
                Color = status.Color,
                Category = status.Category,
                SortOrder = taskStatuses.Count + index,
                IsSystem = true,
            })
            .ToList();

        if (missingStatuses.Count > 0)
        {
            await unitOfWork.Statuses.AddRangeAsync(missingStatuses, cancellationToken);
        }
    }

    private static async Task MergeRelationTypesAsync(
        IReadOnlyList<SetupTemplateRelationDefinition> definitions,
        int workspaceId,
        string ownerId,
        INetptuneUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var existingRelationTypes = await unitOfWork.RelationTypes.GetAllInWorkspace(
            workspaceId,
            includeDeleted: false,
            cancellationToken: cancellationToken);

        var existingKeys = existingRelationTypes
            .Select(relation => relation.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingRelationTypes = definitions
            .Where(relation => !existingKeys.Contains(relation.Key))
            .Select((relation, index) => new RelationType
            {
                WorkspaceId = workspaceId,
                OwnerId = ownerId,
                Name = relation.Name,
                InverseName = relation.InverseName,
                Key = relation.Key,
                Color = relation.Color,
                Category = relation.Category,
                SortOrder = existingRelationTypes.Count + index,
                IsSystem = true,
            })
            .ToList();

        if (missingRelationTypes.Count > 0)
        {
            await unitOfWork.RelationTypes.AddRangeAsync(missingRelationTypes, cancellationToken);
        }
    }

    private static async Task MergeTagsAsync(
        IReadOnlyList<string> definitions,
        int workspaceId,
        string ownerId,
        INetptuneUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var existingTags = await unitOfWork.Tags.GetTagsInWorkspace(
            workspaceId,
            isReadonly: true,
            cancellationToken: cancellationToken);

        var existingNames = existingTags
            .Select(tag => TagNames.Normalize(tag.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingTags = definitions
            .Select(TagNames.Normalize)
            .Where(tag => existingNames.Add(tag))
            .Select(tag => new Tag
            {
                WorkspaceId = workspaceId,
                OwnerId = ownerId,
                Name = tag,
            })
            .ToList();

        if (missingTags.Count > 0)
        {
            await unitOfWork.Tags.AddRangeAsync(missingTags, cancellationToken);
        }
    }

    internal static async Task<IReadOnlyList<CreateBoardGroupOptions>> ResolveBoardGroupsAsync(
        WorkspaceSetupTemplateDefinition template,
        int workspaceId,
        INetptuneUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var statuses = await unitOfWork.Statuses.GetAllInWorkspace(
            workspaceId,
            includeDeleted: false,
            isReadonly: true,
            cancellationToken: cancellationToken);

        var taskStatuses = statuses
            .Where(status => status.EntityType == EntityType.Task)
            .OrderBy(status => status.SortOrder)
            .ThenBy(status => status.Id)
            .ToList();

        var statusesByKey = taskStatuses.ToDictionary(
            status => status.Key,
            StringComparer.OrdinalIgnoreCase);

        return [.. template.BoardGroups
            .Select((group, index) =>
            {
                var status = ResolveStatus(group, statusesByKey, taskStatuses);
                var sortOrder = 1D + index / 10D;

                return new CreateBoardGroupOptions
                {
                    Name = group.Name,
                    SortOrder = sortOrder,
                    StatusId = status?.Id,
                };
            })];
    }

    private static Status? ResolveStatus(
        SetupTemplateBoardGroupDefinition group,
        IReadOnlyDictionary<string, Status> statusesByKey,
        IReadOnlyList<Status> statuses)
    {
        if (group.StatusKey is not null &&
            statusesByKey.TryGetValue(group.StatusKey, out var status))
        {
            return status;
        }

        return group.FallbackStatusCategory.HasValue
            ? statuses.FirstOrDefault(item => item.Category == group.FallbackStatusCategory)
            : null;
    }
}
