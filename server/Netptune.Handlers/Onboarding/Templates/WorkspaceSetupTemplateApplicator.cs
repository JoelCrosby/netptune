using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Onboarding.Templates;
using Netptune.Core.Tags;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Onboarding.Templates;

internal static class WorkspaceSetupTemplateApplicator
{
    internal static async Task ApplyWorkspaceDefaultsAsync(
        WorkspaceSetupTemplateDefinition template,
        int workspaceId,
        string ownerId,
        INetptuneUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var statuses = template.Statuses.Select((status, index) => new Status
        {
            WorkspaceId = workspaceId,
            OwnerId = ownerId,
            EntityType = EntityType.Task,
            Name = status.Name,
            Key = status.Key,
            Color = status.Color,
            Category = status.Category,
            SortOrder = index,
            IsSystem = true,
        });

        var relationTypes = template.RelationTypes.Select((relation, index) => new RelationType
        {
            WorkspaceId = workspaceId,
            OwnerId = ownerId,
            Name = relation.Name,
            InverseName = relation.InverseName,
            Key = relation.Key,
            Color = relation.Color,
            Category = relation.Category,
            SortOrder = index,
            IsSystem = true,
        });

        var tags = template.Tags.Select(tag => new Tag
        {
            WorkspaceId = workspaceId,
            OwnerId = ownerId,
            Name = TagNames.Normalize(tag),
        });

        await unitOfWork.Statuses.AddRangeAsync(statuses, cancellationToken);
        await unitOfWork.RelationTypes.AddRangeAsync(relationTypes, cancellationToken);
        await unitOfWork.Tags.AddRangeAsync(tags, cancellationToken);

        await unitOfWork.CompleteAsync(cancellationToken);
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
        if (group.StatusKey is not null && statusesByKey.TryGetValue(group.StatusKey, out var status))
        {
            return status;
        }

        return group.FallbackStatusCategory.HasValue
            ? statuses.FirstOrDefault(item => item.Category == group.FallbackStatusCategory)
            : null;
    }
}
