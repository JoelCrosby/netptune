using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class UpdateTaskHandler : IActionExecutionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ITaskMutationPipeline TaskMutationPipeline;
    private readonly ILogger<UpdateTaskHandler> Logger;

    public UpdateTaskHandler(
        INetptuneUnitOfWork unitOfWork,
        ITaskMutationPipeline taskMutationPipeline,
        ILogger<UpdateTaskHandler> logger)
    {
        UnitOfWork = unitOfWork;
        TaskMutationPipeline = taskMutationPipeline;
        Logger = logger;
    }

    public AutomationActionType Type => AutomationActionType.UpdateTask;

    public async Task<ActionOutcome> Execute(PlannedAutomationAction action, AutomationPersistenceState state, CancellationToken cancellationToken)
    {
        var contribution = action.Contribution.TaskUpdate;

        if (contribution is null)
        {
            return ActionOutcomes.InvalidContribution();
        }

        var application = await ApplyTaskUpdate(
            action,
            contribution,
            state.TaskMutations,
            cancellationToken);

        if (application.Error is not null)
        {
            return new ActionOutcome(AutomationActionResultStatus.Failed, application.Error);
        }

        if (!application.Changed)
        {
            return new ActionOutcome(AutomationActionResultStatus.Skipped, "The task already has the configured values.");
        }

        return ActionOutcomes.Succeeded();
    }

    private async Task<TaskUpdateApplication> ApplyTaskUpdate(
        PlannedAutomationAction action,
        AutomationTaskUpdateContribution contribution,
        List<TaskMutationOutcome> taskMutations,
        CancellationToken cancellationToken)
    {
        var execution = action.Execution;
        var taskId = execution.Task.Id;
        var previous = await UnitOfWork.Tasks.GetTaskViewModel(taskId, cancellationToken);
        var task = await UnitOfWork.Tasks.GetTaskForUpdate(taskId, cancellationToken);

        if (previous is null || task is null)
        {
            Logger.LogWarning(
                "Automation rule {RuleId} could not update missing or deleted task {TaskId}",
                execution.Rule.Id,
                taskId);

            return TaskUpdateApplication.Failed("The task no longer exists.");
        }

        var boardGroup = await ResolveBoardGroup(contribution.BoardGroupId, cancellationToken);
        var boardGroupMissing = contribution.BoardGroupId.HasValue
            && (boardGroup is null
                || boardGroup.WorkspaceId != execution.Rule.WorkspaceId
                || boardGroup.ProjectId != task.ProjectId);

        if (boardGroupMissing)
        {
            return TaskUpdateApplication.Failed("The selected board group is not available for the task's project.");
        }

        var statusId = contribution.StatusId ?? boardGroup?.StatusId;
        var status = await ResolveStatus(statusId, execution.Rule.WorkspaceId, cancellationToken);

        if (statusId.HasValue && status is null)
        {
            return TaskUpdateApplication.Failed("The selected status is not available in the workspace.");
        }

        var referencedUserIds = GetReferencedUserIds(contribution);
        var validUserIds = await ResolveValidUserIds(referencedUserIds, execution.Rule.WorkspaceId, cancellationToken);
        var hasInvalidUser = referencedUserIds.Any(userId => !validUserIds.Contains(userId));

        if (hasInvalidUser)
        {
            return TaskUpdateApplication.Failed("An owner or assignee is no longer available in the workspace.");
        }

        var requestedTagNames = contribution.AddTags
            .Concat(contribution.RemoveTags)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var tags = await ResolveTags(requestedTagNames, execution.Rule.WorkspaceId, cancellationToken);
        var foundTagNames = tags.Select(tag => tag.Name).ToHashSet(StringComparer.Ordinal);
        var hasInvalidTag = requestedTagNames.Any(tag => !foundTagNames.Contains(tag));

        if (hasInvalidTag)
        {
            return TaskUpdateApplication.Failed("A configured tag is no longer available in the workspace.");
        }

        var sprint = await ResolveSprint(contribution.SprintId, cancellationToken);
        var sprintMissing = contribution.SprintId.HasValue
            && (sprint is null
                || sprint.WorkspaceId != execution.Rule.WorkspaceId
                || sprint.ProjectId != task.ProjectId);

        if (sprintMissing)
        {
            return TaskUpdateApplication.Failed("The selected sprint is not available for the task's project.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var finalStartDate = ResolveFinalDate(task.StartDate, contribution.StartDate, today);
        var finalDueDate = ResolveFinalDate(task.DueDate, contribution.DueDate, today);
        var hasValidSchedule = ProjectTaskSchedule.IsValid(finalStartDate, finalDueDate);

        if (!hasValidSchedule)
        {
            return TaskUpdateApplication.Failed(ProjectTaskSchedule.InvalidDateRangeMessage);
        }

        TaskMutationPipeline.Apply(task, new TaskMutationValues(status, contribution.Priority));

        ApplyScalarUpdates(task, contribution, finalStartDate, finalDueDate);
        ApplyAssignees(task, contribution.AssigneeIds);
        ApplyTags(task, contribution, tags);

        var boardGroupChanged = await ApplyBoardGroup(task.Id, boardGroup, cancellationToken);

        task.UpdatedAt = DateTime.UtcNow;
        task.ModifiedByUserId = execution.ExecutionUserId!;

        await UnitOfWork.CompleteAsync(cancellationToken);

        var current = await UnitOfWork.Tasks.GetTaskViewModel(taskId, cancellationToken);

        if (current is null)
        {
            Logger.LogWarning("Automation rule {RuleId} could not record the update for task {TaskId}", execution.Rule.Id, taskId);

            return TaskUpdateApplication.Failed("The updated task could not be reloaded.");
        }

        var diff = ProjectTaskDiff.Create(previous, current);
        var changed = diff.HasChanges || boardGroupChanged;

        if (!changed)
        {
            return TaskUpdateApplication.Unchanged;
        }

        if (diff.HasChanges)
        {
            var nextChainDepth = execution.ChainDepth + 1;
            var outcome = await TaskMutationPipeline.Record(new TaskMutationRequest
            {
                Previous = previous,
                Current = current,
                Diff = diff,
                ActorUserId = execution.ExecutionUserId!,
                OriginType = EventOriginType.Automation,
                CorrelationId = execution.CorrelationId,
                CausationEventId = execution.CausationEventId,
                AutomationRuleId = execution.Rule.Id,
                AutomationRunId = execution.Run?.Id,
                ChainDepth = nextChainDepth,
            }, cancellationToken);

            taskMutations.Add(outcome);
        }

        var changedFields = diff.ChangedFields.Select(field => field.ToString()).ToList();

        if (boardGroupChanged && !changedFields.Contains("BoardGroup", StringComparer.Ordinal))
        {
            changedFields.Add("BoardGroup");
        }

        action.Result.Output = JsonSerializer.SerializeToDocument(new
        {
            changedFields,
        }, JsonOptions.Default);

        Activity.Current?.AddTag("automation.task_update.applied", taskId);
        SynchronizeExecutionTask(execution.Task, current);

        return TaskUpdateApplication.Succeeded;
    }

    private static void ApplyScalarUpdates(ProjectTask task, AutomationTaskUpdateContribution contribution, DateOnly? startDate, DateOnly? dueDate)
    {
        if (contribution.Name is not null)
        {
            task.Name = contribution.Name.Trim();
        }

        if (contribution.ClearDescription)
        {
            task.Description = null;
        }
        else if (contribution.Description is not null)
        {
            task.Description = contribution.Description;
        }

        if (contribution.ClearOwner)
        {
            task.OwnerId = null;
        }
        else if (contribution.OwnerId is not null)
        {
            task.OwnerId = contribution.OwnerId;
        }

        if (contribution.ClearEstimate)
        {
            task.EstimateType = null;
            task.EstimateValue = null;
        }
        else
        {
            task.EstimateType = contribution.EstimateType ?? task.EstimateType;
            task.EstimateValue = contribution.EstimateValue ?? task.EstimateValue;
        }

        if (contribution.StartDate is not null)
        {
            task.StartDate = startDate;
        }

        if (contribution.DueDate is not null)
        {
            task.DueDate = dueDate;
        }

        if (contribution.ClearSprint)
        {
            task.SprintId = null;
        }
        else if (contribution.SprintId.HasValue)
        {
            task.SprintId = contribution.SprintId;
        }
    }

    private static void ApplyAssignees(ProjectTask task, IReadOnlyCollection<string>? assigneeIds)
    {
        if (assigneeIds is null)
        {
            return;
        }

        task.ProjectTaskAppUsers = ProjectTaskAppUser.MergeUsersIds(
            task.Id,
            task.ProjectTaskAppUsers,
            assigneeIds).ToList();
    }

    private static void ApplyTags(
        ProjectTask task,
        AutomationTaskUpdateContribution contribution,
        IReadOnlyCollection<Tag> tags)
    {
        if (tags.Count == 0)
        {
            return;
        }

        var addTagNames = contribution.AddTags.ToHashSet(StringComparer.Ordinal);
        var removeTagNames = contribution.RemoveTags.ToHashSet(StringComparer.Ordinal);
        var addTagIds = tags.Where(tag => addTagNames.Contains(tag.Name)).Select(tag => tag.Id);
        var removeTagIds = tags.Where(tag => removeTagNames.Contains(tag.Name)).Select(tag => tag.Id).ToHashSet();
        var selectedTagIds = task.ProjectTaskTags
            .Select(taskTag => taskTag.TagId)
            .Where(tagId => !removeTagIds.Contains(tagId))
            .Concat(addTagIds)
            .Distinct()
            .ToList();

        task.ProjectTaskTags = ProjectTaskTag.MergeTagIds(
            task.Id,
            task.ProjectTaskTags,
            selectedTagIds).ToList();
    }

    private async Task<bool> ApplyBoardGroup(int taskId, BoardGroupTaskTarget? boardGroup, CancellationToken cancellationToken)
    {
        if (boardGroup is null)
        {
            return false;
        }

        var currentGroup = await UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(taskId, cancellationToken);
        var alreadyInGroup = currentGroup?.BoardGroupId == boardGroup.Id;

        if (alreadyInGroup)
        {
            return false;
        }

        await UnitOfWork.ProjectTasksInGroups.DeleteAllByTaskId([taskId], cancellationToken);
        await UnitOfWork.ProjectTasksInGroups.AddAsync(new ProjectTaskInBoardGroup
        {
            ProjectTaskId = taskId,
            BoardGroupId = boardGroup.Id,
            SortOrder = boardGroup.MaxSortOrder + 1,
        }, cancellationToken);

        return true;
    }

    private static List<string> GetReferencedUserIds(AutomationTaskUpdateContribution contribution)
    {
        var userIds = contribution.AssigneeIds?.ToList() ?? [];

        if (contribution.OwnerId is not null)
        {
            userIds.Add(contribution.OwnerId);
        }

        return userIds.Distinct(StringComparer.Ordinal).ToList();
    }

    private async Task<BoardGroupTaskTarget?> ResolveBoardGroup(int? boardGroupId, CancellationToken cancellationToken)
    {
        if (!boardGroupId.HasValue)
        {
            return null;
        }

        var boardGroup = await UnitOfWork.BoardGroups.GetTaskTarget(boardGroupId.Value, cancellationToken);

        return boardGroup;
    }

    private async Task<Status?> ResolveStatus(
        int? statusId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (!statusId.HasValue)
        {
            return null;
        }

        var status = await UnitOfWork.Statuses.GetInWorkspace(
            statusId.Value,
            workspaceId,
            cancellationToken: cancellationToken);

        return status;
    }

    private async Task<HashSet<string>> ResolveValidUserIds(
        List<string> referencedUserIds,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (referencedUserIds.Count == 0)
        {
            return [];
        }

        var validUsers = await UnitOfWork.Users.IsUserInWorkspaceRange(
            referencedUserIds,
            workspaceId,
            cancellationToken);
        var validUserIds = validUsers.Select(user => user.Id).ToHashSet(StringComparer.Ordinal);

        return validUserIds;
    }

    private async Task<List<Tag>> ResolveTags(
        List<string> requestedTagNames,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (requestedTagNames.Count == 0)
        {
            return [];
        }

        var tags = await UnitOfWork.Tags.GetTagsByValueInWorkspace(
            workspaceId,
            requestedTagNames,
            true,
            cancellationToken);

        return tags;
    }

    private async Task<Sprint?> ResolveSprint(int? sprintId, CancellationToken cancellationToken)
    {
        if (!sprintId.HasValue)
        {
            return null;
        }

        var sprint = await UnitOfWork.Sprints.GetAsync(sprintId.Value, true, cancellationToken);

        return sprint;
    }

    private static DateOnly? ResolveFinalDate(
        DateOnly? currentDate,
        AutomationDateUpdate? configuredUpdate,
        DateOnly today)
    {
        if (configuredUpdate is null)
        {
            return currentDate;
        }

        var finalDate = ResolveDate(configuredUpdate, today);

        return finalDate;
    }

    private static DateOnly? ResolveDate(AutomationDateUpdate update, DateOnly today)
    {
        return update.Mode switch
        {
            AutomationDateUpdateMode.Absolute => update.Date,
            AutomationDateUpdateMode.RelativeDays => today.AddDays(update.Offset ?? 0),
            AutomationDateUpdateMode.RelativeBusinessDays => AddBusinessDays(today, update.Offset ?? 0),
            AutomationDateUpdateMode.Clear => null,
            _ => null,
        };
    }

    private static DateOnly AddBusinessDays(DateOnly date, int offset)
    {
        var remaining = Math.Abs(offset);
        var direction = Math.Sign(offset);
        var result = date;

        while (remaining > 0)
        {
            result = result.AddDays(direction);
            var isWeekday = result.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

            if (isWeekday)
            {
                remaining--;
            }
        }

        return result;
    }

    private static void SynchronizeExecutionTask(ProjectTask executionTask, TaskViewModel current)
    {
        executionTask.Name = current.Name;
        executionTask.Description = current.Description;
        executionTask.StatusId = current.StatusId;
        executionTask.OwnerId = current.OwnerId;
        executionTask.Priority = current.Priority;
        executionTask.EstimateType = current.EstimateType;
        executionTask.EstimateValue = current.EstimateValue;
        executionTask.StartDate = current.StartDate;
        executionTask.DueDate = current.DueDate;
        executionTask.SprintId = current.SprintId;
    }

    private sealed record TaskUpdateApplication
    {
        public bool Changed { get; init; }

        public string? Error { get; init; }

        public static TaskUpdateApplication Succeeded { get; } = new()
        {
            Changed = true,
        };

        public static TaskUpdateApplication Unchanged { get; } = new();

        public static TaskUpdateApplication Failed(string error)
        {
            return new TaskUpdateApplication
            {
                Error = error,
            };
        }
    }
}
