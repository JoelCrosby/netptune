using Mediator;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record BulkUpdateTasksCommand(BulkUpdateTasksRequest Request) : IRequest<ClientResponse>;

public sealed class BulkUpdateTasksCommandHandler : IRequestHandler<BulkUpdateTasksCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly ILogger<BulkUpdateTasksCommandHandler> Logger;
    private readonly IEventRecordWriter EventRecords;

    public BulkUpdateTasksCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ILogger<BulkUpdateTasksCommandHandler> logger,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Logger = logger;
        EventRecords = eventRecords;
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

        var taskIds = await UnitOfWork.Tasks.GetValidTaskIdsInWorkspace(
            requestedTaskIds,
            workspaceId,
            cancellationToken);
        var missingTaskIds = requestedTaskIds.Except(taskIds).ToList();

        if (missingTaskIds.Count > 0)
        {
            return ClientResponse.Failed(
                $"Tasks were not found in the workspace: {string.Join(", ", missingTaskIds)}");
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

        var assigneeIds = req.AssigneeIds?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (req.AssigneeIds is not null)
        {
            var containsInvalidAssignee = assigneeIds!.Count != req.AssigneeIds.Count;

            if (containsInvalidAssignee)
            {
                return ClientResponse.Failed("Assignee IDs cannot be empty or duplicated");
            }

            var assignees = await UnitOfWork.Users.IsUserInWorkspaceRange(
                assigneeIds,
                workspaceId,
                cancellationToken);
            var validAssigneeIds = assignees.Select(assignee => assignee.Id).ToHashSet(StringComparer.Ordinal);
            var missingAssigneeIds = assigneeIds.Where(id => !validAssigneeIds.Contains(id)).ToList();

            if (missingAssigneeIds.Count > 0)
            {
                return ClientResponse.Failed(
                    $"Assignees were not found in the workspace: {string.Join(", ", missingAssigneeIds)}");
            }
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

        await UnitOfWork.Transaction(async () =>
        {
            foreach (var task in tasks)
            {
                var oldStatusId = task.StatusId;
                var oldStatusCategory = task.Status.Category;
                var oldEstimateType = task.EstimateType;
                var oldEstimateValue = task.EstimateValue;
                var oldSprintId = task.SprintId;
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

                if (projectChanged)
                {
                    MoveToProject(
                        task,
                        req.ProjectId!.Value,
                        nextProjectScopeId!.Value);
                    nextProjectScopeId++;
                }

                if (req.AssigneeIds is not null)
                {
                    task.ProjectTaskAppUsers = ProjectTaskAppUser.MergeUsersIds(
                        task.Id,
                        task.ProjectTaskAppUsers,
                        assigneeIds!).ToList();
                }

                // Moving a task to a different project invalidates its board-group
                // membership, which belongs to the old project's board.

                if (projectChanged)
                {
                    await RepositionInBoardGroup(task, cancellationToken);
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
                    await EventRecords.Append(new EventWriteRequest<FieldTransitionedPayload>
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
                    }, cancellationToken);
                }

                if (oldEstimateType != task.EstimateType || oldEstimateValue != task.EstimateValue)
                {
                    await EventRecords.Append(new EventWriteRequest<FieldTransitionedPayload>
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
                    }, cancellationToken);

                    if (task.SprintId.HasValue && await IsActiveSprint(task.SprintId.Value, cancellationToken))
                    {
                        await EventRecords.Append(new EventWriteRequest<ScopeMemberAttributeChangedPayload>
                        {
                            WorkspaceId = workspaceId,
                            EventKey = EventKeys.ScopeMemberAttributeChanged,
                            SubjectType = EventEntityTypes.From(EntityType.Sprint),
                            SubjectId = task.SprintId.Value.ToString(),
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
                        }, cancellationToken);
                    }
                }

                if (oldSprintId != task.SprintId)
                {

                    if (oldSprintId.HasValue && await IsActiveSprint(oldSprintId.Value, cancellationToken))
                    {
                        await AppendScopeChange(
                            task,
                            oldSprintId.Value,
                            "removed",
                            workspaceId,
                            cancellationToken);
                    }

                    if (task.SprintId.HasValue && await IsActiveSprint(task.SprintId.Value, cancellationToken))
                    {
                        await AppendScopeChange(
                            task,
                            task.SprintId.Value,
                            "added",
                            workspaceId,
                            cancellationToken);
                    }
                }
            }

            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        return ClientResponse.Success;
    }

    private async Task<bool> IsActiveSprint(int sprintId, CancellationToken cancellationToken)
    {
        var sprint = await UnitOfWork.Sprints.GetAsync(sprintId, true, cancellationToken);

        return sprint?.Status == SprintStatus.Active;
    }

    private Task<EventRecord> AppendScopeChange(
        ProjectTask task,
        int sprintId,
        string change,
        int workspaceId,
        CancellationToken cancellationToken) => EventRecords.Append(new EventWriteRequest<ScopeMemberChangedPayload>
        {
            WorkspaceId = workspaceId,
            EventKey = EventKeys.ScopeMemberChanged,
            SubjectType = EventEntityTypes.From(EntityType.Sprint),
            SubjectId = sprintId.ToString(),
            Payload = new ScopeMemberChangedPayload
            {
                Change = change,
                MemberType = EventEntityTypes.From(EntityType.Task),
                MemberId = task.Id.ToString(),
                EstimateType = task.EstimateType?.ToString(),
                EstimateValue = task.EstimateValue,
                StatusId = task.StatusId,
                StatusCategory = task.Status.Category.ToString(),
            },
            References =
        [
            new EventReferenceInput
            {
                Role = EventReferenceRoles.Member,
                EntityType = EventEntityTypes.From(EntityType.Task),
                EntityId = task.Id.ToString(),
            },
            new EventReferenceInput
            {
                Role = EventReferenceRoles.Scope,
                EntityType = EventEntityTypes.From(EntityType.Project),
                EntityId = task.ProjectId!.Value.ToString(),
            },
        ],
        }, cancellationToken);

    private static void MoveToProject(ProjectTask task, int projectId, int projectScopeId)
    {
        task.ProjectScopeId = projectScopeId;
        task.ProjectId = projectId;
    }

    private async Task RepositionInBoardGroup(ProjectTask task, CancellationToken cancellationToken)
    {

        if (task.ProjectId is null)
        {
            return;
        }

        var group = await UnitOfWork.BoardGroups.GetDefaultTaskTarget(task.ProjectId.Value, cancellationToken);

        if (group is null)
        {
            Logger.LogInformation(
                "Project with id {ProjectId} does not have a default board group",
                task.ProjectId.Value);

            return;
        }

        await UnitOfWork.ProjectTasksInGroups.DeleteAllByTaskId([task.Id], cancellationToken);

        await UnitOfWork.ProjectTasksInGroups.AddAsync(new ProjectTaskInBoardGroup
        {
            BoardGroupId = group.Id,
            ProjectTaskId = task.Id,
            SortOrder = group.MaxSortOrder + 1,
        }, cancellationToken);
    }
}
