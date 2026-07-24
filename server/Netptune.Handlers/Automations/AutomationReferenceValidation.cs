using Netptune.Core.Enums;
using Netptune.Core.Requests;

namespace Netptune.Handlers.Automations;

internal static class AutomationReferenceValidation
{
    public static async Task<string?> Validate(AutomationValidationContext context, CancellationToken cancellationToken)
    {
        var updates = context.Request.Actions
            .Where(action => action.Type == AutomationActionType.UpdateTask)
            .ToList();

        foreach (var update in updates)
        {
            var error = await ValidateUpdate(update, context, cancellationToken);

            if (error is not null)
            {
                return error;
            }
        }

        return null;
    }

    private static async Task<string?> ValidateUpdate(AutomationActionRequest update, AutomationValidationContext context, CancellationToken cancellationToken)
    {
        if (update.StatusId.HasValue)
        {
            var status = await context.UnitOfWork.Statuses.GetInWorkspace(
                update.StatusId.Value,
                context.WorkspaceId,
                true,
                cancellationToken);

            if (status is null)
            {
                return $"Status with id {update.StatusId.Value} was not found in the workspace.";
            }
        }

        var userIds = update.AssigneeIds?.ToList() ?? [];

        if (update.OwnerId is not null)
        {
            userIds.Add(update.OwnerId);
        }

        if (userIds.Count > 0)
        {
            var users = await context.UnitOfWork.Users.IsUserInWorkspaceRange(
                userIds,
                context.WorkspaceId,
                cancellationToken);
            var validUserIds = users.Select(user => user.Id).ToHashSet(StringComparer.Ordinal);
            var missingUserIds = userIds.Where(userId => !validUserIds.Contains(userId)).ToList();

            if (missingUserIds.Count > 0)
            {
                return $"Users were not found in the workspace: {string.Join(", ", missingUserIds)}";
            }
        }

        var tagNames = update.AddTags
            .Concat(update.RemoveTags)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (tagNames.Count > 0)
        {
            var tags = await context.UnitOfWork.Tags.GetTagsByValueInWorkspace(
                context.WorkspaceId,
                tagNames,
                true,
                cancellationToken);
            var validTagNames = tags.Select(tag => tag.Name).ToHashSet(StringComparer.Ordinal);
            var missingTagNames = tagNames.Where(tag => !validTagNames.Contains(tag)).ToList();

            if (missingTagNames.Count > 0)
            {
                return $"Tags were not found in the workspace: {string.Join(", ", missingTagNames)}";
            }
        }

        if (update.SprintId.HasValue)
        {
            var sprint = await context.UnitOfWork.Sprints.GetAsync(update.SprintId.Value, true, cancellationToken);
            var sprintIsInvalid = sprint is null || sprint.WorkspaceId != context.WorkspaceId;

            if (sprintIsInvalid)
            {
                return $"Sprint with id {update.SprintId.Value} was not found in the workspace.";
            }
        }

        if (update.BoardGroupId.HasValue)
        {
            var boardGroup = await context.UnitOfWork.BoardGroups.GetTaskTarget(
                update.BoardGroupId.Value,
                cancellationToken);
            var boardGroupIsInvalid = boardGroup is null || boardGroup.WorkspaceId != context.WorkspaceId;

            if (boardGroupIsInvalid)
            {
                return $"Board group with id {update.BoardGroupId.Value} was not found in the workspace.";
            }
        }

        return null;
    }
}
