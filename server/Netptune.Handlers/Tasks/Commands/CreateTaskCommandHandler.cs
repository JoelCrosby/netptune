using Mediator;

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
using Netptune.Core.Services.Relations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record CreateTaskCommand(AddProjectTaskRequest Request) : IRequest<ClientResponse<TaskViewModel>>;

public sealed class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, ClientResponse<TaskViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;
    private readonly IEventRecordWriter EventRecords;
    private readonly ITaskRelationLinker RelationLinker;
    private readonly ITaskReferenceResolver ReferenceResolver;
    private readonly ITaskStatusResolver StatusResolver;

    public CreateTaskCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        IEventPublisher eventPublisher,
        IEventRecordWriter eventRecords,
        ITaskRelationLinker relationLinker,
        ITaskReferenceResolver referenceResolver,
        ITaskStatusResolver statusResolver)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
        EventRecords = eventRecords;
        RelationLinker = relationLinker;
        ReferenceResolver = referenceResolver;
        StatusResolver = statusResolver;
    }

    public async ValueTask<ClientResponse<TaskViewModel>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var hasValidSchedule = ProjectTaskSchedule.IsValid(req.StartDate, req.DueDate);

        if (!hasValidSchedule)
        {
            return ClientResponse<TaskViewModel>.Failed(ProjectTaskSchedule.InvalidDateRangeMessage);
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (!workspaceId.HasValue)
        {
            return ClientResponse<TaskViewModel>.Failed($"workspace with key {workspaceKey} not found");
        }

        var user = await Identity.GetCurrentUser();
        var project = await UnitOfWork.Projects.GetTaskCreationProject(req.ProjectId!.Value, workspaceId.Value, cancellationToken);

        if (project is null)
        {
            return ClientResponse<TaskViewModel>.Failed($"Project with Id {req.ProjectId} not found");
        }

        var requestedAssignees = ReadRequestedAssignees(req);
        var assigneeResolution = await ReferenceResolver.ResolveAssignees(
            requestedAssignees,
            workspaceId.Value,
            cancellationToken);

        if (!assigneeResolution.IsValid)
        {
            return ClientResponse<TaskViewModel>.Failed(assigneeResolution.Error);
        }

        var tagResolution = await ReferenceResolver.ResolveTags(req.Tags, workspaceId.Value, cancellationToken);

        if (!tagResolution.IsValid)
        {
            return ClientResponse<TaskViewModel>.Failed(tagResolution.Error);
        }

        // A task with nobody named still belongs to whoever raised it.
        var hasNamedAssignee = assigneeResolution.UserIds.Count > 0;
        IReadOnlyList<string> assigneeIds = hasNamedAssignee ? assigneeResolution.UserIds : [user.Id];

        // Planned before the task row exists, so a rejected link cannot leave a task behind without it.
        var relationPlan = await RelationLinker.Plan(new TaskRelationPlanRequest
        {
            WorkspaceId = workspaceId.Value,
            WorkspaceKey = workspaceKey,
            Links = req.Relations ?? [],
        }, cancellationToken);

        if (!relationPlan.IsValid)
        {
            return ClientResponse<TaskViewModel>.Failed(relationPlan.Error);
        }

        await UnitOfWork.Statuses.EnsureNewTaskStatus(workspaceId.Value, user.Id, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var status = await ResolveStatus(req, project, workspaceId.Value, cancellationToken);

        if (status is null)
        {
            return ClientResponse<TaskViewModel>.Failed("Task status not found");
        }

        Sprint? targetSprint = null;

        if (req.SprintId.HasValue)
        {
            targetSprint = await UnitOfWork.Sprints.GetAsync(req.SprintId.Value, true, cancellationToken);

            if (targetSprint is null || targetSprint.WorkspaceId != workspaceId.Value || targetSprint.ProjectId != project.Id || targetSprint.Status == SprintStatus.Completed)
            {
                return ClientResponse<TaskViewModel>.Failed($"Sprint with Id {req.SprintId} not found");
            }
        }

        var task = new ProjectTask
        {
            Name = req.Name,
            Description = req.Description,
            StatusId = status.Id,
            Status = status,
            ProjectId = req.ProjectId,
            SprintId = req.SprintId,
            OwnerId = user.Id,
            WorkspaceId = workspaceId.Value,
            Priority = req.Priority,
            EstimateType = req.EstimateType,
            EstimateValue = req.EstimateValue,
            StartDate = req.StartDate,
            DueDate = req.DueDate,
            ProjectTaskAppUsers = assigneeIds
                .Select(assigneeId => new ProjectTaskAppUser { UserId = assigneeId })
                .ToList(),
            ProjectTaskTags = tagResolution.Tags
                .Select(tag => new ProjectTaskTag { TagId = tag.Id })
                .ToList(),
        };

        if (req.BoardGroupId.HasValue)
        {
            await AddTaskToBoardGroup(req.BoardGroupId.Value, task, cancellationToken);
        }
        else
        {
            await AddTaskToBoardGroup(project, task, cancellationToken);
        }

        var scopeId = await UnitOfWork.Projects.ReserveTaskScopeIds(project.Id, 1, cancellationToken);

        if (!scopeId.HasValue)
        {
            return ClientResponse<TaskViewModel>.Failed($"Unable to get scope id for project with id {project.Id}");
        }

        task.ProjectScopeId = scopeId.Value;

        var result = await UnitOfWork.Tasks.AddAsync(task, cancellationToken);
        var linkedRelations = new List<LinkedTaskRelation>();

        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.CompleteAsync(cancellationToken);

            var links = await RelationLinker.Apply(relationPlan, result.Id, cancellationToken);

            linkedRelations.AddRange(links);

            var creationReferences = new List<EventReferenceInput>
            {
                new EventReferenceInput
                {
                    Role = EventReferenceRoles.Scope,
                    EntityType = EventEntityTypes.From(EntityType.Project),
                    EntityId = task.ProjectId!.Value.ToString(),
                },
            };

            if (task.SprintId.HasValue)
            {
                creationReferences.Add(new EventReferenceInput
                {
                    Role = EventReferenceRoles.Scope,
                    EntityType = EventEntityTypes.From(EntityType.Sprint),
                    EntityId = task.SprintId.Value.ToString(),
                });
            }

            await EventRecords.Append(new EventWriteRequest<EntityCreatedPayload>
            {
                WorkspaceId = task.WorkspaceId,
                EventKey = EventKeys.EntityCreated,
                SubjectType = EventEntityTypes.From(EntityType.Task),
                SubjectId = result.Id.ToString(),
                Payload = new EntityCreatedPayload
                {
                    Name = task.Name,
                    StatusId = task.StatusId,
                    StatusCategory = status.Category.ToString(),
                    SprintId = task.SprintId,
                    EstimateType = task.EstimateType?.ToString(),
                    EstimateValue = task.EstimateValue,
                },
                References = creationReferences,
            }, cancellationToken);

            if (task.SprintId.HasValue && targetSprint?.Status == SprintStatus.Active)
            {
                var scope = new SprintScope(task.WorkspaceId, task.SprintId.Value, task.ProjectId!.Value);
                var member = new SprintMember
                {
                    TaskId = result.Id,
                    StatusId = task.StatusId,
                    StatusCategory = status.Category.ToString(),
                    EstimateType = task.EstimateType?.ToString(),
                    EstimateValue = task.EstimateValue,
                };

                var added = SprintMemberEvents.Changed(scope, member, SprintMemberChanges.Added);

                await EventRecords.Append(added, cancellationToken);
            }

            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        var response = await UnitOfWork.Tasks.GetTaskViewModel(result.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = result.Id;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Create;
        });

        await EventPublisher.IndexTasks([result.Id], workspaceKey);

        await EventPublisher.Dispatch(new TaskCreatedMessage
        {
            WorkspaceId = task.WorkspaceId,
            TaskId = result.Id,
            ActorUserId = user.Id,
        });

        await RelationLinker.Publish(linkedRelations, user.Id);

        return ClientResponse<TaskViewModel>.Success(response!);
    }

    private static List<string> ReadRequestedAssignees(AddProjectTaskRequest request)
    {
        if (request.AssigneeIds is not null)
        {
            return request.AssigneeIds;
        }

        return request.AssigneeId is null ? [] : [request.AssigneeId];
    }

    private async Task AddTaskToBoardGroup(int groupId, ProjectTask task, CancellationToken cancellationToken)
    {
        var boardGroup = await UnitOfWork.BoardGroups.GetTaskTarget(groupId, cancellationToken);

        if (boardGroup is null)
        {
            throw new Exception($"BoardGroup with id of {groupId} does not exist.");
        }

        if (boardGroup.StatusId.HasValue)
        {
            var status = await UnitOfWork.Statuses.GetInWorkspace(boardGroup.StatusId.Value, task.WorkspaceId, cancellationToken: cancellationToken);

            if (status is not null)
            {
                task.StatusId = status.Id;
                task.Status = status;
            }
        }

        task.ProjectTaskInBoardGroups.Add(new ProjectTaskInBoardGroup
        {
            SortOrder = boardGroup.MaxSortOrder + 1,
            BoardGroupId = boardGroup.Id,
            ProjectTask = task,
        });
    }

    private async Task AddTaskToBoardGroup(TaskCreationProject project, ProjectTask task, CancellationToken cancellationToken)
    {
        var boardGroup = await UnitOfWork.BoardGroups.GetDefaultTaskTarget(project.Id, cancellationToken);

        if (boardGroup is null)
        {
            throw new Exception($"Project '{project.Name}' With Id {project.Id} does not have a default board group.");
        }

        task.ProjectTaskInBoardGroups.Add(new ProjectTaskInBoardGroup
        {
            SortOrder = boardGroup.MaxSortOrder + 1,
            BoardGroupId = boardGroup.Id,
            ProjectTask = task,
        });
    }

    // A status the request names is found or the create fails; without one the project's default leads
    // the fallback chain.
    private Task<Status?> ResolveStatus(
        AddProjectTaskRequest request,
        TaskCreationProject project,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (request.StatusId.HasValue)
        {
            return StatusResolver.ResolveRequested(request.StatusId.Value, workspaceId, cancellationToken);
        }

        return StatusResolver.ResolveDefault(project.DefaultStatusId, workspaceId, cancellationToken);
    }
}
