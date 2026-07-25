using System.Text.Json;

using Netptune.Automation.Matching;

using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Core.Services.Automations;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Automation.Actions;

internal sealed class CreateTaskAction : IAutomationAction
{
    public AutomationActionType Type => AutomationActionType.CreateTask;

    public IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>
    {
        NetptunePermissions.Tasks.Create,
    };

    public IReadOnlySet<string> GetRequiredPermissions(AutomationActionRequest request)
    {
        var assignsUsers = request.CopyAssignees || request.AssigneeIds is { Count: > 0 };

        return BuildRequiredPermissions(assignsUsers, request.AddTags.Count > 0, request.SprintId.HasValue);
    }

    public IReadOnlySet<string> GetRequiredPermissions(AutomationAction action)
    {
        var assignsUsers = JsonUtils.ReadObject<bool>(action.Config, "copyAssignees")
            || JsonUtils.ReadList<string>(action.Config, "assigneeIds").Count > 0;
        var addsTags = JsonUtils.ReadList<string>(action.Config, "addTags").Count > 0;
        var setsSprint = JsonUtils.ReadInt(action.Config, "sprintId").HasValue;

        return BuildRequiredPermissions(assignsUsers, addsTags, setsSprint);
    }

    public string? Validate(AutomationActionRequest request)
    {
        var hasName = !string.IsNullOrWhiteSpace(request.TaskName);

        if (!hasName)
        {
            return "Create task actions require a task name.";
        }

        var nameError = AutomationMessageTemplate.Validate(request.TaskName);

        if (nameError is not null)
        {
            return nameError;
        }

        var descriptionError = AutomationMessageTemplate.Validate(request.TaskDescription);

        if (descriptionError is not null)
        {
            return descriptionError;
        }

        var hasConflictingAssignees = request.CopyAssignees && request.AssigneeIds is { Count: > 0 };

        if (hasConflictingAssignees)
        {
            return "Create task actions cannot copy assignees and set assignees at the same time.";
        }

        var hasInvalidAssignees = request.AssigneeIds is not null
            && ContainsBlankOrDuplicate(request.AssigneeIds);

        if (hasInvalidAssignees)
        {
            return "Create task actions require distinct assignees.";
        }

        var hasInvalidTags = ContainsBlankOrDuplicate(request.AddTags);

        if (hasInvalidTags)
        {
            return "Create task actions require distinct tags.";
        }

        return ValidateDate(request.StartDate, "start date") ?? ValidateDate(request.DueDate, "due date");
    }

    public JsonDocument CreateConfig(AutomationActionRequest request)
    {
        return JsonSerializer.SerializeToDocument(new
        {
            name = request.TaskName,
            description = request.TaskDescription,
            statusId = request.StatusId,
            priority = request.Priority,
            assigneeIds = request.AssigneeIds ?? [],
            copyAssignees = request.CopyAssignees,
            addTags = request.AddTags,
            startDate = request.StartDate,
            dueDate = request.DueDate,
            sprintId = request.SprintId,
            boardGroupId = request.BoardGroupId,
            linkRelationTypeId = request.LinkRelationTypeId,
        }, JsonOptions.Default);
    }

    public AutomationActionViewModel ToViewModel(AutomationAction action)
    {
        return new AutomationActionViewModel
        {
            Id = action.Id,
            Type = action.Type,
            SortOrder = action.SortOrder,
            TaskName = JsonUtils.ReadString(action.Config, "name"),
            TaskDescription = JsonUtils.ReadString(action.Config, "description"),
            StatusId = JsonUtils.ReadInt(action.Config, "statusId"),
            Priority = JsonUtils.ReadEnum<TaskPriority>(action.Config, "priority"),
            AssigneeIds = JsonUtils.ReadList<string>(action.Config, "assigneeIds"),
            CopyAssignees = JsonUtils.ReadObject<bool>(action.Config, "copyAssignees"),
            AddTags = JsonUtils.ReadList<string>(action.Config, "addTags"),
            StartDate = JsonUtils.ReadObject<AutomationDateUpdate>(action.Config, "startDate"),
            DueDate = JsonUtils.ReadObject<AutomationDateUpdate>(action.Config, "dueDate"),
            SprintId = JsonUtils.ReadInt(action.Config, "sprintId"),
            BoardGroupId = JsonUtils.ReadInt(action.Config, "boardGroupId"),
            LinkRelationTypeId = JsonUtils.ReadInt(action.Config, "linkRelationTypeId"),
        };
    }

    public AutomationActionPlanContribution Plan(AutomationActionPlanningContext context)
    {
        var action = context.Action;
        var task = context.Task;
        var rule = context.Rule;
        var today = AutomationTimeZones.Today(rule, DateTime.UtcNow);
        var configuredName = JsonUtils.ReadString(action.Config, "name") ?? task.Name;
        var configuredDescription = JsonUtils.ReadString(action.Config, "description");
        var startDate = JsonUtils.ReadObject<AutomationDateUpdate>(action.Config, "startDate");
        var dueDate = JsonUtils.ReadObject<AutomationDateUpdate>(action.Config, "dueDate");

        return new AutomationActionPlanContribution
        {
            TaskCreation = new AutomationTaskCreationContribution
            {
                Name = AutomationMessageTemplate.Render(configuredName, task, rule),
                Description = configuredDescription is null
                    ? null
                    : AutomationMessageTemplate.Render(configuredDescription, task, rule),
                StatusId = JsonUtils.ReadInt(action.Config, "statusId"),
                Priority = JsonUtils.ReadEnum<TaskPriority>(action.Config, "priority"),
                AssigneeIds = ResolveAssignees(context),
                AddTags = JsonUtils.ReadList<string>(action.Config, "addTags"),
                StartDate = startDate is null ? null : AutomationDateResolver.Resolve(startDate, today),
                DueDate = dueDate is null ? null : AutomationDateResolver.Resolve(dueDate, today),
                SprintId = JsonUtils.ReadInt(action.Config, "sprintId"),
                BoardGroupId = JsonUtils.ReadInt(action.Config, "boardGroupId"),
                LinkRelationTypeId = JsonUtils.ReadInt(action.Config, "linkRelationTypeId"),
            },
        };
    }

    private static List<string> ResolveAssignees(AutomationActionPlanningContext context)
    {
        var copyAssignees = JsonUtils.ReadObject<bool>(context.Action.Config, "copyAssignees");

        if (copyAssignees)
        {
            return context.Task.ProjectTaskAppUsers
                .Select(assignee => assignee.UserId)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        return JsonUtils.ReadList<string>(context.Action.Config, "assigneeIds");
    }

    private static IReadOnlySet<string> BuildRequiredPermissions(bool assignsUsers, bool addsTags, bool setsSprint)
    {
        var permissions = new HashSet<string>
        {
            NetptunePermissions.Tasks.Create,
        };

        if (assignsUsers)
        {
            permissions.Add(NetptunePermissions.Tasks.Reassign);
        }

        if (addsTags)
        {
            permissions.Add(NetptunePermissions.Tags.Assign);
        }

        if (setsSprint)
        {
            permissions.Add(NetptunePermissions.Sprints.ManageTasks);
        }

        return permissions;
    }

    private static bool ContainsBlankOrDuplicate(IReadOnlyCollection<string> values)
    {
        var hasBlank = values.Any(string.IsNullOrWhiteSpace);
        var hasDuplicate = values.Distinct(StringComparer.Ordinal).Count() != values.Count;

        return hasBlank || hasDuplicate;
    }

    private static string? ValidateDate(AutomationDateUpdate? update, string field)
    {
        if (update is null)
        {
            return null;
        }

        var isDefinedMode = Enum.IsDefined(update.Mode);

        if (!isDefinedMode)
        {
            return $"Create task actions have an unsupported {field} mode.";
        }

        var requiresDate = update.Mode == AutomationDateUpdateMode.Absolute;

        if (requiresDate && update.Date is null)
        {
            return $"Create task actions require a {field}.";
        }

        var requiresOffset = update.Mode is AutomationDateUpdateMode.RelativeDays
            or AutomationDateUpdateMode.RelativeBusinessDays;
        var hasValidOffset = update.Offset is >= -3650 and <= 3650;

        if (requiresOffset && !hasValidOffset)
        {
            return $"Create task actions require a {field} offset between -3650 and 3650 days.";
        }

        return null;
    }
}
