using System.Text.Json;

using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Core.Services.Automations;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Automation.Actions;

internal sealed class UpdateTaskAction : IAutomationAction
{
    public AutomationActionType Type => AutomationActionType.UpdateTask;

    public IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>
    {
        NetptunePermissions.Tasks.Update,
    };

    public IReadOnlySet<string> GetRequiredPermissions(AutomationActionRequest request)
    {
        var updatesAssignees = request.OwnerId is not null
            || request.ClearOwner
            || request.AssigneeIds is not null;

        var updatesTags = request.AddTags.Count > 0 || request.RemoveTags.Count > 0;
        var updatesSprint = request.SprintId.HasValue || request.ClearSprint;
        var updatesBoardGroup = request.BoardGroupId.HasValue;

        return BuildRequiredPermissions(updatesAssignees, updatesTags, updatesSprint, updatesBoardGroup);
    }

    public IReadOnlySet<string> GetRequiredPermissions(AutomationAction action)
    {
        var updatesAssignees = JsonUtils.ReadString(action.Config, "ownerId") is not null
            || JsonUtils.ReadObject<bool>(action.Config, "clearOwner")
            || JsonUtils.ReadObject<List<string>?>(action.Config, "assigneeIds") is not null;

        var updatesTags = JsonUtils.ReadList<string>(action.Config, "addTags").Count > 0
            || JsonUtils.ReadList<string>(action.Config, "removeTags").Count > 0;

        var updatesSprint = JsonUtils.ReadInt(action.Config, "sprintId").HasValue
            || JsonUtils.ReadObject<bool>(action.Config, "clearSprint");

        var updatesBoardGroup = JsonUtils.ReadInt(action.Config, "boardGroupId").HasValue;

        return BuildRequiredPermissions(updatesAssignees, updatesTags, updatesSprint, updatesBoardGroup);
    }

    public string? Validate(AutomationActionRequest request)
    {
        var hasUpdate = HasUpdate(request);

        if (!hasUpdate)
        {
            return "Update task actions require at least one field update.";
        }

        var hasInvalidName = request.TaskName is not null
            && string.IsNullOrWhiteSpace(request.TaskName);

        if (hasInvalidName)
        {
            return "Update task action names cannot be empty.";
        }

        var hasOwnerConflict = request.ClearOwner && request.OwnerId is not null;

        if (hasOwnerConflict)
        {
            return "Update task actions cannot set and clear the owner.";
        }

        var hasDescriptionConflict = request.ClearDescription && request.TaskDescription is not null;

        if (hasDescriptionConflict)
        {
            return "Update task actions cannot set and clear the description.";
        }

        var hasEstimateConflict = request.ClearEstimate
            && (request.EstimateType.HasValue || request.EstimateValue.HasValue);

        if (hasEstimateConflict)
        {
            return "Update task actions cannot set and clear the estimate.";
        }

        var estimateValueMissingType = request.EstimateValue.HasValue
            && !request.EstimateType.HasValue;

        if (estimateValueMissingType)
        {
            return "Update task action estimate values require an estimate type.";
        }

        var hasSprintConflict = request.ClearSprint && request.SprintId.HasValue;

        if (hasSprintConflict)
        {
            return "Update task actions cannot set and clear the sprint.";
        }

        var invalidAssignee = request.AssigneeIds is not null
            && ContainsBlankOrDuplicate(request.AssigneeIds);

        if (invalidAssignee)
        {
            return "Update task action assignee IDs cannot be empty or duplicated.";
        }

        var invalidTags = ContainsBlankOrDuplicate(request.AddTags)
            || ContainsBlankOrDuplicate(request.RemoveTags);

        if (invalidTags)
        {
            return "Update task action tags cannot be empty or duplicated.";
        }

        var conflictingTags = request.AddTags
            .Intersect(request.RemoveTags, StringComparer.Ordinal)
            .Any();

        if (conflictingTags)
        {
            return "Update task actions cannot add and remove the same tag.";
        }

        return ValidateDate(request.StartDate, "start date") ?? ValidateDate(request.DueDate, "due date");
    }

    public JsonDocument CreateConfig(AutomationActionRequest request)
    {
        return JsonSerializer.SerializeToDocument(new
        {
            statusId = request.StatusId,
            priority = request.Priority,
            name = request.TaskName,
            description = request.TaskDescription,
            clearDescription = request.ClearDescription,
            ownerId = request.OwnerId,
            clearOwner = request.ClearOwner,
            assigneeIds = request.AssigneeIds,
            addTags = request.AddTags,
            removeTags = request.RemoveTags,
            startDate = request.StartDate,
            dueDate = request.DueDate,
            estimateType = request.EstimateType,
            estimateValue = request.EstimateValue,
            clearEstimate = request.ClearEstimate,
            sprintId = request.SprintId,
            clearSprint = request.ClearSprint,
            boardGroupId = request.BoardGroupId,
        }, JsonOptions.Default);
    }

    public AutomationActionViewModel ToViewModel(AutomationAction action)
    {
        return new AutomationActionViewModel
        {
            Id = action.Id,
            Type = action.Type,
            SortOrder = action.SortOrder,
            StatusId = JsonUtils.ReadInt(action.Config, "statusId"),
            Priority = JsonUtils.ReadEnum<TaskPriority>(action.Config, "priority"),
            TaskName = JsonUtils.ReadString(action.Config, "name"),
            TaskDescription = JsonUtils.ReadString(action.Config, "description"),
            ClearDescription = JsonUtils.ReadObject<bool>(action.Config, "clearDescription"),
            OwnerId = JsonUtils.ReadString(action.Config, "ownerId"),
            ClearOwner = JsonUtils.ReadObject<bool>(action.Config, "clearOwner"),
            AssigneeIds = JsonUtils.ReadObject<List<string>?>(action.Config, "assigneeIds"),
            AddTags = JsonUtils.ReadList<string>(action.Config, "addTags"),
            RemoveTags = JsonUtils.ReadList<string>(action.Config, "removeTags"),
            StartDate = JsonUtils.ReadObject<AutomationDateUpdate>(action.Config, "startDate"),
            DueDate = JsonUtils.ReadObject<AutomationDateUpdate>(action.Config, "dueDate"),
            EstimateType = JsonUtils.ReadEnum<EstimateType>(action.Config, "estimateType"),
            EstimateValue = JsonUtils.ReadObject<decimal?>(action.Config, "estimateValue"),
            ClearEstimate = JsonUtils.ReadObject<bool>(action.Config, "clearEstimate"),
            SprintId = JsonUtils.ReadInt(action.Config, "sprintId"),
            ClearSprint = JsonUtils.ReadObject<bool>(action.Config, "clearSprint"),
            BoardGroupId = JsonUtils.ReadInt(action.Config, "boardGroupId"),
        };
    }

    public AutomationActionPlanContribution Plan(AutomationActionPlanningContext context)
    {
        var action = context.Action;
        var taskUpdate = new AutomationTaskUpdateContribution
        {
            StatusId = JsonUtils.ReadInt(action.Config, "statusId"),
            Priority = JsonUtils.ReadEnum<TaskPriority>(action.Config, "priority"),
            Name = JsonUtils.ReadString(action.Config, "name"),
            Description = JsonUtils.ReadString(action.Config, "description"),
            ClearDescription = JsonUtils.ReadObject<bool>(action.Config, "clearDescription"),
            OwnerId = JsonUtils.ReadString(action.Config, "ownerId"),
            ClearOwner = JsonUtils.ReadObject<bool>(action.Config, "clearOwner"),
            AssigneeIds = JsonUtils.ReadObject<List<string>?>(action.Config, "assigneeIds"),
            AddTags = JsonUtils.ReadList<string>(action.Config, "addTags"),
            RemoveTags = JsonUtils.ReadList<string>(action.Config, "removeTags"),
            StartDate = JsonUtils.ReadObject<AutomationDateUpdate>(action.Config, "startDate"),
            DueDate = JsonUtils.ReadObject<AutomationDateUpdate>(action.Config, "dueDate"),
            EstimateType = JsonUtils.ReadEnum<EstimateType>(action.Config, "estimateType"),
            EstimateValue = JsonUtils.ReadObject<decimal?>(action.Config, "estimateValue"),
            ClearEstimate = JsonUtils.ReadObject<bool>(action.Config, "clearEstimate"),
            SprintId = JsonUtils.ReadInt(action.Config, "sprintId"),
            ClearSprint = JsonUtils.ReadObject<bool>(action.Config, "clearSprint"),
            BoardGroupId = JsonUtils.ReadInt(action.Config, "boardGroupId"),
        };

        return new AutomationActionPlanContribution
        {
            TaskUpdate = taskUpdate,
        };
    }

    private static bool HasUpdate(AutomationActionRequest request)
    {
        return request.StatusId.HasValue
            || request.Priority.HasValue
            || request.TaskName is not null
            || request.TaskDescription is not null
            || request.ClearDescription
            || request.OwnerId is not null
            || request.ClearOwner
            || request.AssigneeIds is not null
            || request.AddTags.Count > 0
            || request.RemoveTags.Count > 0
            || request.StartDate is not null
            || request.DueDate is not null
            || request.EstimateType.HasValue
            || request.EstimateValue.HasValue
            || request.ClearEstimate
            || request.SprintId.HasValue
            || request.ClearSprint
            || request.BoardGroupId.HasValue;
    }

    private static IReadOnlySet<string> BuildRequiredPermissions(
        bool updatesAssignees,
        bool updatesTags,
        bool updatesSprint,
        bool updatesBoardGroup)
    {
        var permissions = new HashSet<string>
        {
            NetptunePermissions.Tasks.Update,
        };

        if (updatesAssignees)
        {
            permissions.Add(NetptunePermissions.Tasks.Reassign);
        }

        if (updatesTags)
        {
            permissions.Add(NetptunePermissions.Tags.Assign);
        }

        if (updatesSprint)
        {
            permissions.Add(NetptunePermissions.Sprints.ManageTasks);
        }

        if (updatesBoardGroup)
        {
            permissions.Add(NetptunePermissions.Tasks.Move);
        }

        return permissions;
    }

    private static bool ContainsBlankOrDuplicate(IReadOnlyCollection<string> values)
    {
        var normalizedValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return normalizedValues.Count != values.Count;
    }

    private static string? ValidateDate(AutomationDateUpdate? update, string field)
    {
        if (update is null)
        {
            return null;
        }

        var hasSupportedMode = Enum.IsDefined(update.Mode);

        if (!hasSupportedMode)
        {
            return $"Update task action {field} mode is not supported.";
        }

        var absoluteDateMissing = update.Mode == AutomationDateUpdateMode.Absolute && !update.Date.HasValue;

        if (absoluteDateMissing)
        {
            return $"Update task action {field} requires a date.";
        }

        var relativeOffsetMissing = update.Mode is AutomationDateUpdateMode.RelativeDays
            or AutomationDateUpdateMode.RelativeBusinessDays
            && !update.Offset.HasValue;

        if (relativeOffsetMissing)
        {
            return $"Update task action {field} requires a relative offset.";
        }

        var relativeOffsetTooLarge = Math.Abs(update.Offset ?? 0) > 3650;

        if (relativeOffsetTooLarge)
        {
            return $"Update task action {field} offset cannot exceed 3650 days.";
        }

        return null;
    }
}
