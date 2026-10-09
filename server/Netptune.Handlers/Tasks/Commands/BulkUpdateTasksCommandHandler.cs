using Mediator;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Sprints;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record BulkUpdateTasksCommand(BulkUpdateTasksRequest Request) : IRequest<ClientResponse>;

public sealed class BulkUpdateTasksCommandHandler : IRequestHandler<BulkUpdateTasksCommand, ClientResponse>
{
    private const int MissingIdsNamedInError = 10;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly ILogger<BulkUpdateTasksCommandHandler> Logger;
    private readonly IEventRecordWriter EventRecords;
    private readonly IEventPublisher EventPublisher;
    private readonly ITaskPlacementService Placement;
    private readonly ITaskReferenceResolver ReferenceResolver;

    public BulkUpdateTasksCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ILogger<BulkUpdateTasksCommandHandler> logger,
        IEventRecordWriter eventRecords,
        IEventPublisher eventPublisher,
        ITaskPlacementService placement,
        ITaskReferenceResolver referenceResolver)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Logger = logger;
        EventRecords = eventRecords;
        EventPublisher = eventPublisher;
        Placement = placement;
        ReferenceResolver = referenceResolver;
    }

    public async ValueTask<ClientResponse> Handle(BulkUpdateTasksCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var workspaceId = await Identity.GetWorkspaceId();
        var workspaceKey = Identity.GetWorkspaceKey();
        var requestedTaskIds = req.TaskIds.Distinct().ToList();

        if (requestedTaskIds.Count == 0)
        {
            return ClientResponse.Failed("At least one task is required");
        }

        var overflow = RequestLimits.DescribeBulkIdOverflow(requestedTaskIds.Count);

        if (overflow is not null)
        {
            return ClientResponse.Failed(overflow);
        }

        var taskIds = await UnitOfWork.Tasks.GetValidTaskIdsInWorkspace(
            requestedTaskIds,
            workspaceId,
            cancellationToken);
        var missingTaskIds = requestedTaskIds.Except(taskIds).ToList();

        if (missingTaskIds.Count > 0)
        {
            var namedIds = string.Join(", ", missingTaskIds.Take(MissingIdsNamedInError));
            var remainder = missingTaskIds.Count - MissingIdsNamedInError;
            var suffix = remainder > 0 ? $" and {remainder} more" : string.Empty;

            return ClientResponse.Failed($"Tasks were not found in the workspace: {namedIds}{suffix}");
        }

        var tasks = await UnitOfWork.Tasks.GetTasksForUpdate(taskIds, cancellationToken);
        var status = req.StatusId.HasValue
            ? await UnitOfWork.Statuses.GetInWorkspace(req.StatusId.Value, workspaceId, cancellationToken: cancellationToken)
            : null;

        if (req.StatusId.HasValue && status is null)
        {
            return ClientResponse.Failed($"Status with id {req.StatusId.Value} was not found in the workspace");
        }

        if (req.ClearSprint && req.SprintId.HasValue)
        {
            return ClientResponse.Failed("SprintId and ClearSprint cannot both be supplied");
        }

        if (req.ClearDueDate && req.DueDate.HasValue)
        {
            return ClientResponse.Failed("DueDate and ClearDueDate cannot both be supplied");
        }

        var dueDateBreaksSchedule = req.DueDate.HasValue
            && tasks.Any(task => !ProjectTaskSchedule.IsValid(task.StartDate, req.DueDate));

        if (dueDateBreaksSchedule)
        {
            return ClientResponse.Failed(ProjectTaskSchedule.InvalidDateRangeMessage);
        }

        if (req.SprintId.HasValue)
        {
            var sprint = await UnitOfWork.Sprints.GetTaskAssignmentTarget(
                workspaceKey,
                req.SprintId.Value,
                cancellationToken);

            if (sprint is null)
            {
                return ClientResponse.Failed($"Sprint with id {req.SprintId.Value} was not found in the workspace");
            }

            var sprintAcceptsTasks = sprint.Status is SprintStatus.Planning or SprintStatus.Active;

            if (!sprintAcceptsTasks)
            {
                return ClientResponse.Failed("Completed or cancelled sprints cannot be changed");
            }

            var targetProjectIds = tasks
                .Select(task => req.ProjectId ?? task.ProjectId)
                .Distinct()
                .ToList();
            var allTasksBelongToSprintProject = targetProjectIds.Count == 1
                && targetProjectIds[0] == sprint.ProjectId;

            if (!allTasksBelongToSprintProject)
            {
                return ClientResponse.Failed("Every task must belong to the sprint's project");
            }
        }

        var assigneeUpdate = await ReferenceResolver.ResolveAssignees(req.AssigneeIds, workspaceId, cancellationToken);

        if (!assigneeUpdate.IsValid)
        {
            return ClientResponse.Failed(assigneeUpdate.Error);
        }

        var tagUpdate = await ReferenceResolver.ResolveTags(req.Tags, workspaceId, cancellationToken);

        if (!tagUpdate.IsValid)
        {
            return ClientResponse.Failed(tagUpdate.Error);
        }

        var targetProjectId = req.ProjectId;
        var movedTaskCount = targetProjectId.HasValue
            ? tasks.Count(task => task.ProjectId != targetProjectId.Value)
            : 0;
        var nextProjectScopeId = movedTaskCount > 0
            ? await UnitOfWork.Projects.ReserveTaskScopeIds(
                targetProjectId!.Value,
                movedTaskCount,
                cancellationToken)
            : null;
        var scopeReservationFailed = movedTaskCount > 0 && !nextProjectScopeId.HasValue;

        if (scopeReservationFailed)
        {
            return ClientResponse.Failed($"Project with Id {targetProjectId} not found");
        }

        var activeSprintIds = await GetActiveSprintIds(tasks, req.SprintId, cancellationToken);
        var events = new List<IEventWriteRequest>();
        var movedTaskIds = new List<int>();

        await UnitOfWork.Transaction(async () =>
        {
            foreach (var task in tasks)
            {
                var oldStatusId = task.StatusId;
                var oldStatusCategory = task.Status!.Category;
                var oldEstimateType = task.EstimateType;
                var oldEstimateValue = task.EstimateValue;
                var oldSprintId = task.SprintId;
                var oldAssigneeIds = task.ProjectTaskAppUsers.Select(assignment => assignment.UserId).ToList();
                var projectChanged = req.ProjectId.HasValue && task.ProjectId != req.ProjectId.Value;

                if (status is not null)
                {
                    task.StatusId = status.Id;
                    task.Status = status;
                }

                if (req.Priority.HasValue)
                {
                    task.Priority = req.Priority;
                }

                if (req.EstimateType.HasValue)
                {
                    task.EstimateType = req.EstimateType;
                }

                if (req.EstimateValue.HasValue)
                {
                    task.EstimateValue = req.EstimateValue;
                }

                if (req.ClearSprint)
                {
                    task.SprintId = null;
                }
                else if (req.SprintId.HasValue)
                {
                    task.SprintId = req.SprintId;
                }

                if (req.ClearDueDate)
                {
                    task.DueDate = null;
                }
                else if (req.DueDate.HasValue)
                {
                    task.DueDate = req.DueDate;
                }

                if (projectChanged)
                {
                    MoveToProject(
                        task,
                        req.ProjectId!.Value,
                        nextProjectScopeId!.Value);
                    nextProjectScopeId++;
                }

                if (assigneeUpdate.ShouldUpdate)
                {
                    var targetAssigneeIds = ResolveAssigneeTargets(task, assigneeUpdate.UserIds, req.AssigneeMode);

                    task.ProjectTaskAppUsers = ProjectTaskAppUser.MergeUsersIds(
                        task.Id,
                        task.ProjectTaskAppUsers,
                        targetAssigneeIds).ToList();
                }

                if (tagUpdate.ShouldUpdate)
                {
                    var targetTagIds = ResolveTagTargets(task, tagUpdate.Tags, req.TagMode);

                    task.ProjectTaskTags = ProjectTaskTag.MergeTagIds(
                        task.Id,
                        task.ProjectTaskTags,
                        targetTagIds).ToList();
                }

                if (projectChanged)
                {
                    movedTaskIds.Add(task.Id);
                }

                var references = new List<EventReferenceInput>();

                if (task.ProjectId.HasValue)
                {
                    references.Add(new EventReferenceInput
                    {
                        Role = EventReferenceRoles.Scope,
                        EntityType = EventEntityTypes.From(EntityType.Project),
                        EntityId = task.ProjectId.Value.ToString(),
                    });
                }

                if (task.SprintId.HasValue)
                {
                    references.Add(new EventReferenceInput
                    {
                        Role = EventReferenceRoles.Scope,
                        EntityType = EventEntityTypes.From(EntityType.Sprint),
                        EntityId = task.SprintId.Value.ToString(),
                    });
                }

                if (oldStatusId != task.StatusId && status is not null)
                {
                    events.Add(new EventWriteRequest<FieldTransitionedPayload>
                    {
                        WorkspaceId = workspaceId,
                        EventKey = EventKeys.EntityFieldTransitioned,
                        SubjectType = EventEntityTypes.From(EntityType.Task),
                        SubjectId = task.Id.ToString(),
                        Payload = new FieldTransitionedPayload
                        {
                            Field = "status",
                            OldValue = oldStatusId.ToString(),
                            NewValue = task.StatusId.ToString(),
                            OldCategory = oldStatusCategory.ToString(),
                            NewCategory = status.Category.ToString(),
                        },
                        References = references,
                    });
                }

                if (oldEstimateType != task.EstimateType || oldEstimateValue != task.EstimateValue)
                {
                    events.Add(new EventWriteRequest<FieldTransitionedPayload>
                    {
                        WorkspaceId = workspaceId,
                        EventKey = EventKeys.EntityFieldTransitioned,
                        SubjectType = EventEntityTypes.From(EntityType.Task),
                        SubjectId = task.Id.ToString(),
                        Payload = new FieldTransitionedPayload
                        {
                            Field = "estimate",
                            OldUnit = oldEstimateType?.ToString(),
                            NewUnit = task.EstimateType?.ToString(),
                            OldNumericValue = oldEstimateValue,
                            NewNumericValue = task.EstimateValue,
                        },
                        References = references,
                    });

                    var isInActiveSprint = task.SprintId.HasValue && activeSprintIds.Contains(task.SprintId.Value);

                    if (isInActiveSprint)
                    {
                        events.Add(new EventWriteRequest<ScopeMemberAttributeChangedPayload>
                        {
                            WorkspaceId = workspaceId,
                            EventKey = EventKeys.ScopeMemberAttributeChanged,
                            SubjectType = EventEntityTypes.From(EntityType.Sprint),
                            SubjectId = task.SprintId!.Value.ToString(),
                            Payload = new ScopeMemberAttributeChangedPayload
                            {
                                MemberType = EventEntityTypes.From(EntityType.Task),
                                MemberId = task.Id.ToString(),
                                Field = "estimate",
                                OldUnit = oldEstimateType?.ToString(),
                                NewUnit = task.EstimateType?.ToString(),
                                OldNumericValue = oldEstimateValue,
                                NewNumericValue = task.EstimateValue,
                            },
                            References =
                            [
                                new EventReferenceInput
                                {
                                    Role = EventReferenceRoles.Member,
                                    EntityType = EventEntityTypes.From(EntityType.Task),
                                    EntityId = task.Id.ToString(),
                                },
                                ..references,
                            ],
                        });
                    }
                }

                if (assigneeUpdate.ShouldUpdate)
                {
                    var assigneeChange = new TaskAssigneeChange(task, oldAssigneeIds, workspaceId);

                    events.AddRange(BuildAssigneeChanges(assigneeChange, references));
                }

                if (oldSprintId != task.SprintId)
                {
                    var leftActiveSprint = oldSprintId.HasValue && activeSprintIds.Contains(oldSprintId.Value);
                    var joinedActiveSprint = task.SprintId.HasValue && activeSprintIds.Contains(task.SprintId.Value);

                    if (leftActiveSprint)
                    {
                        events.Add(BuildScopeChange(task, oldSprintId!.Value, SprintMemberChanges.Removed, workspaceId));
                    }

                    if (joinedActiveSprint)
                    {
                        events.Add(BuildScopeChange(task, task.SprintId!.Value, SprintMemberChanges.Added, workspaceId));
                    }
                }
            }

            await EventRecords.AppendRange(events, cancellationToken);
            await RepositionInBoardGroup(movedTaskIds, req.ProjectId, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        await EventPublisher.IndexTasks(taskIds, workspaceKey);

        return ClientResponse.Success;
    }

    private sealed record TaskAssigneeChange(ProjectTask Task, List<string> PreviousAssigneeIds, int WorkspaceId);

    private static IEnumerable<EventWriteRequest<FieldTransitionedPayload>> BuildAssigneeChanges(
        TaskAssigneeChange change,
        List<EventReferenceInput> references)
    {
        var task = change.Task;
        var currentAssigneeIds = task.ProjectTaskAppUsers.Select(assignment => assignment.UserId).ToList();
        var addedUserIds = currentAssigneeIds.Except(change.PreviousAssigneeIds, StringComparer.Ordinal);
        var removedUserIds = change.PreviousAssigneeIds.Except(currentAssigneeIds, StringComparer.Ordinal);
        var template = new FieldTransitionedPayload { Field = TaskAssigneeTransitions.Field };
        var transitions = TaskAssigneeTransitions.Split(template, addedUserIds, removedUserIds);

        return transitions.Select(transition => new EventWriteRequest<FieldTransitionedPayload>
        {
            WorkspaceId = change.WorkspaceId,
            EventKey = EventKeys.EntityFieldTransitioned,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = task.Id.ToString(),
            Payload = transition,
            References = references,
        });
    }

    private async Task<HashSet<int>> GetActiveSprintIds(
        List<ProjectTask> tasks,
        int? targetSprintId,
        CancellationToken cancellationToken)
    {
        var currentSprintIds = tasks
            .Where(task => task.SprintId.HasValue)
            .Select(task => task.SprintId!.Value);
        var targetSprintIds = targetSprintId.HasValue ? [targetSprintId.Value] : Array.Empty<int>();
        var involvedSprintIds = currentSprintIds.Concat(targetSprintIds).Distinct().ToList();

        if (involvedSprintIds.Count == 0)
        {
            return [];
        }

        var sprints = await UnitOfWork.Sprints.GetAllByIdAsync(involvedSprintIds, true, cancellationToken);
        var activeSprintIds = sprints
            .Where(sprint => sprint.Status == SprintStatus.Active)
            .Select(sprint => sprint.Id)
            .ToHashSet();

        return activeSprintIds;
    }

    private static EventWriteRequest<ScopeMemberChangedPayload> BuildScopeChange(
        ProjectTask task,
        int sprintId,
        string change,
        int workspaceId)
    {
        var scope = new SprintScope(workspaceId, sprintId, task.ProjectId!.Value);
        var member = new SprintMember
        {
            TaskId = task.Id,
            StatusId = task.StatusId,
            StatusCategory = task.Status!.Category.ToString(),
            EstimateType = task.EstimateType?.ToString(),
            EstimateValue = task.EstimateValue,
        };

        return SprintMemberEvents.Changed(scope, member, change);
    }

    private static List<string> ResolveAssigneeTargets(
        ProjectTask task,
        IReadOnlyList<string> userIds,
        BulkCollectionMode mode)
    {
        if (mode is BulkCollectionMode.Replace)
        {
            return userIds.ToList();
        }

        var existingUserIds = task.ProjectTaskAppUsers.Select(assignment => assignment.UserId);

        return existingUserIds.Concat(userIds).Distinct(StringComparer.Ordinal).ToList();
    }

    private static List<int> ResolveTagTargets(ProjectTask task, IReadOnlyList<Tag> tags, BulkCollectionMode mode)
    {
        var selectedTagIds = tags.Select(tag => tag.Id);

        if (mode is BulkCollectionMode.Replace)
        {
            return selectedTagIds.ToList();
        }

        var existingTagIds = task.ProjectTaskTags.Select(link => link.TagId);

        return existingTagIds.Concat(selectedTagIds).Distinct().ToList();
    }

    private static void MoveToProject(ProjectTask task, int projectId, int projectScopeId)
    {
        task.ProjectScopeId = projectScopeId;
        task.ProjectId = projectId;
    }

    // Moving a task to a different project invalidates its board-group membership, which belongs to
    // the old project's board.
    private async Task RepositionInBoardGroup(
        List<int> movedTaskIds,
        int? targetProjectId,
        CancellationToken cancellationToken)
    {
        if (movedTaskIds.Count == 0 || targetProjectId is null)
        {
            return;
        }

        var group = await UnitOfWork.BoardGroups.GetDefaultTaskTarget(targetProjectId.Value, cancellationToken);

        if (group is null)
        {
            Logger.LogInformation(
                "Project with id {ProjectId} does not have a default board group",
                targetProjectId.Value);

            return;
        }

        await Placement.ReplaceAllPlacements(movedTaskIds, group, cancellationToken);
    }
}
