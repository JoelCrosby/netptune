using System.Text.Json;

using Netptune.Automation.Common;
using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Automations;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Relations;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Relationships;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class CreateTaskHandler : IActionExecutionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventRecordWriter EventRecords;
    private readonly IEventPublisher EventPublisher;

    public CreateTaskHandler(
        INetptuneUnitOfWork unitOfWork,
        IEventRecordWriter eventRecords,
        IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        EventRecords = eventRecords;
        EventPublisher = eventPublisher;
    }

    public AutomationActionType Type => AutomationActionType.CreateTask;

    public async Task<ActionOutcome> Execute(
        PlannedAutomationAction action,
        AutomationPersistenceState state,
        CancellationToken cancellationToken)
    {
        var contribution = action.Contribution.TaskCreation;

        if (contribution is null)
        {
            return ActionOutcomes.InvalidContribution();
        }

        var execution = action.Execution;
        var sourceTask = execution.Task;
        var workspaceId = execution.Rule.WorkspaceId;

        if (!sourceTask.ProjectId.HasValue)
        {
            return new ActionOutcome(AutomationActionResultStatus.Failed, "The triggering task is not in a project.");
        }

        var resolution = await ResolveCreationTargets(
            contribution,
            sourceTask,
            sourceTask.ProjectId.Value,
            workspaceId,
            cancellationToken);

        if (resolution.Error is not null)
        {
            return new ActionOutcome(AutomationActionResultStatus.Failed, resolution.Error);
        }

        var scopeId = await UnitOfWork.Projects.ReserveTaskScopeIds(sourceTask.ProjectId.Value, 1, cancellationToken);

        if (!scopeId.HasValue)
        {
            return new ActionOutcome(AutomationActionResultStatus.Failed, "Unable to reserve a task key for the project.");
        }

        var task = BuildTask(action, contribution, resolution, scopeId.Value);

        await UnitOfWork.Tasks.AddAsync(task, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var relationError = await LinkToSourceTask(action, contribution, task, cancellationToken);

        if (relationError is not null)
        {
            return new ActionOutcome(AutomationActionResultStatus.Failed, relationError);
        }

        await AppendCreationEvent(action, task, resolution.Status!, cancellationToken);
        await PublishCreation(action, task);

        action.Result.Output = JsonSerializer.SerializeToDocument(new
        {
            createdTaskId = task.Id,
            createdTaskKey = $"{resolution.ProjectKey}-{task.ProjectScopeId}",
        }, JsonOptions.Default);

        return ActionOutcomes.Succeeded();
    }

    private ProjectTask BuildTask(
        PlannedAutomationAction action,
        AutomationTaskCreationContribution contribution,
        TaskCreationResolution resolution,
        int scopeId)
    {
        var execution = action.Execution;
        var sourceTask = execution.Task;
        var executionUserId = execution.ExecutionUserId!;
        var task = new ProjectTask
        {
            Name = contribution.Name,
            Description = contribution.Description,
            StatusId = resolution.Status!.Id,
            ProjectId = sourceTask.ProjectId,
            SprintId = contribution.SprintId,
            WorkspaceId = execution.Rule.WorkspaceId,
            Priority = contribution.Priority,
            StartDate = contribution.StartDate,
            DueDate = contribution.DueDate,
            ProjectScopeId = scopeId,
            OwnerId = executionUserId,
            CreatedByUserId = executionUserId,
        };

        foreach (var assigneeId in contribution.AssigneeIds)
        {
            task.ProjectTaskAppUsers.Add(new ProjectTaskAppUser
            {
                UserId = assigneeId,
            });
        }

        foreach (var tag in resolution.Tags)
        {
            task.ProjectTaskTags.Add(new ProjectTaskTag
            {
                TagId = tag.Id,
            });
        }

        task.ProjectTaskInBoardGroups.Add(new ProjectTaskInBoardGroup
        {
            SortOrder = resolution.BoardGroupSortOrder,
            BoardGroupId = resolution.BoardGroupId!.Value,
            ProjectTask = task,
        });

        return task;
    }

    private async Task<TaskCreationResolution> ResolveCreationTargets(
        AutomationTaskCreationContribution contribution,
        ProjectTask sourceTask,
        int projectId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var project = await UnitOfWork.Projects.GetTaskCreationProject(projectId, workspaceId, cancellationToken);

        if (project is null)
        {
            return TaskCreationResolution.Failed("The triggering task's project is no longer available.");
        }

        var boardGroup = await ResolveBoardGroup(contribution.BoardGroupId, projectId, cancellationToken);

        if (boardGroup is null)
        {
            return TaskCreationResolution.Failed("The selected board group is not available for the project.");
        }

        var statusId = contribution.StatusId ?? boardGroup.StatusId ?? project.DefaultStatusId;
        var status = await ResolveStatus(statusId, workspaceId, cancellationToken);

        if (status is null)
        {
            return TaskCreationResolution.Failed("A task status is not available in the workspace.");
        }

        var validUserIds = await ResolveValidUserIds(contribution.AssigneeIds, workspaceId, cancellationToken);
        var hasInvalidUser = contribution.AssigneeIds.Any(userId => !validUserIds.Contains(userId));

        if (hasInvalidUser)
        {
            return TaskCreationResolution.Failed("An assignee is no longer available in the workspace.");
        }

        var tags = await ResolveTags(contribution.AddTags, workspaceId, cancellationToken);
        var foundTagNames = tags.Select(tag => tag.Name).ToHashSet(StringComparer.Ordinal);
        var hasInvalidTag = contribution.AddTags.Any(tag => !foundTagNames.Contains(tag));

        if (hasInvalidTag)
        {
            return TaskCreationResolution.Failed("A configured tag is no longer available in the workspace.");
        }

        var sprintError = await ValidateSprint(contribution.SprintId, projectId, workspaceId, cancellationToken);

        if (sprintError is not null)
        {
            return TaskCreationResolution.Failed(sprintError);
        }

        return new TaskCreationResolution
        {
            Status = status,
            Tags = tags,
            BoardGroupId = boardGroup.Id,
            BoardGroupSortOrder = boardGroup.MaxSortOrder + 1,
            ProjectKey = sourceTask.Project?.Key,
        };
    }

    private async Task<string?> ValidateSprint(
        int? sprintId,
        int projectId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (!sprintId.HasValue)
        {
            return null;
        }

        var sprint = await UnitOfWork.Sprints.GetAsync(sprintId.Value, true, cancellationToken);
        var isAvailable = sprint is not null
            && sprint.WorkspaceId == workspaceId
            && sprint.ProjectId == projectId
            && sprint.Status != SprintStatus.Completed;

        if (!isAvailable)
        {
            return "The selected sprint is not available for the project.";
        }

        return null;
    }

    private async Task<BoardGroupTaskTarget?> ResolveBoardGroup(
        int? boardGroupId,
        int projectId,
        CancellationToken cancellationToken)
    {
        if (!boardGroupId.HasValue)
        {
            return await UnitOfWork.BoardGroups.GetDefaultTaskTarget(projectId, cancellationToken);
        }

        return await UnitOfWork.BoardGroups.GetTaskTarget(boardGroupId.Value, cancellationToken);
    }

    private async Task<Status?> ResolveStatus(int? statusId, int workspaceId, CancellationToken cancellationToken)
    {
        if (statusId.HasValue)
        {
            var status = await UnitOfWork.Statuses.GetInWorkspace(
                statusId.Value,
                workspaceId,
                cancellationToken: cancellationToken);

            if (status is not null)
            {
                return status;
            }
        }

        var newStatus = await UnitOfWork.Statuses.GetTaskStatusByKey(workspaceId, "new", cancellationToken);

        if (newStatus is not null)
        {
            return newStatus;
        }

        return await UnitOfWork.Statuses.GetFirstTaskStatus(workspaceId, cancellationToken);
    }

    private async Task<HashSet<string>> ResolveValidUserIds(
        List<string> userIds,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        var users = await UnitOfWork.Users.IsUserInWorkspaceRange(userIds, workspaceId, cancellationToken);

        return users.Select(user => user.Id).ToHashSet(StringComparer.Ordinal);
    }

    private async Task<List<Tag>> ResolveTags(
        List<string> tagNames,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (tagNames.Count == 0)
        {
            return [];
        }

        return await UnitOfWork.Tags.GetTagsByValueInWorkspace(
            workspaceId,
            tagNames,
            cancellationToken: cancellationToken);
    }

    private async Task<string?> LinkToSourceTask(
        PlannedAutomationAction action,
        AutomationTaskCreationContribution contribution,
        ProjectTask createdTask,
        CancellationToken cancellationToken)
    {
        if (!contribution.LinkRelationTypeId.HasValue)
        {
            return null;
        }

        var execution = action.Execution;
        var workspaceId = execution.Rule.WorkspaceId;
        var relationType = await UnitOfWork.RelationTypes.GetInWorkspace(
            contribution.LinkRelationTypeId.Value,
            workspaceId,
            cancellationToken: cancellationToken);

        if (relationType is null)
        {
            return "The selected relation type is no longer available in the workspace.";
        }

        var hasSingleSource = RelationTypeRules.HasSingleSource(relationType.Category);

        if (hasSingleSource)
        {
            var hasExistingSource = await UnitOfWork.ProjectTaskRelations.HasExistingSource(
                relationType.Id,
                createdTask.Id,
                cancellationToken);

            if (hasExistingSource)
            {
                return "The created task already has a relation of the selected type.";
            }
        }

        var relation = new ProjectTaskRelation
        {
            WorkspaceId = workspaceId,
            RelationTypeId = relationType.Id,
            SourceTaskId = execution.Task.Id,
            TargetTaskId = createdTask.Id,
        };

        await UnitOfWork.ProjectTaskRelations.AddAsync(relation, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return null;
    }

    private async Task AppendCreationEvent(
        PlannedAutomationAction action,
        ProjectTask task,
        Status status,
        CancellationToken cancellationToken)
    {
        await EventRecords.Append(new EventWriteRequest<EntityCreatedPayload>
        {
            WorkspaceId = task.WorkspaceId,
            EventKey = EventKeys.EntityCreated,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = task.Id.ToString(),
            ActorUserId = action.Execution.ExecutionUserId!,
            Payload = new EntityCreatedPayload
            {
                Name = task.Name,
                StatusId = task.StatusId,
                StatusCategory = status.Category.ToString(),
                SprintId = task.SprintId,
            },
            References =
            [
                new EventReferenceInput
                {
                    Role = EventReferenceRoles.Scope,
                    EntityType = EventEntityTypes.From(EntityType.Project),
                    EntityId = task.ProjectId!.Value.ToString(),
                },
            ],
        }, cancellationToken);
    }

    private async Task PublishCreation(PlannedAutomationAction action, ProjectTask task)
    {
        var execution = action.Execution;

        await EventPublisher.Dispatch(new SearchIndexEvent
        {
            Operation = SearchIndexOperation.Index,
            EntityType = "task",
            EntityIds = [task.Id],
            WorkspaceSlug = execution.Task.Workspace.Slug,
        });

        await EventPublisher.Dispatch(new TaskCreatedMessage
        {
            WorkspaceId = task.WorkspaceId,
            TaskId = task.Id,
            ActorUserId = execution.ExecutionUserId!,
            OriginType = EventOriginType.Automation,
            AutomationRuleId = execution.Rule.Id,
            AutomationRunId = execution.Run?.Id,
            CorrelationId = execution.CorrelationId,
            ChainDepth = execution.ChainDepth + 1,
        });
    }

    private sealed record TaskCreationResolution
    {
        public Status? Status { get; init; }

        public List<Tag> Tags { get; init; } = [];

        public int? BoardGroupId { get; init; }

        public double BoardGroupSortOrder { get; init; }

        public string? ProjectKey { get; init; }

        public string? Error { get; init; }

        public static TaskCreationResolution Failed(string error)
        {
            return new TaskCreationResolution { Error = error };
        }
    }
}
