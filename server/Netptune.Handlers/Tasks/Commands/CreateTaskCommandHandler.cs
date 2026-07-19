using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
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

    public CreateTaskCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        IEventPublisher eventPublisher,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
        EventRecords = eventRecords;
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
        var userId = req.AssigneeId ?? user.Id;
        var project = await UnitOfWork.Projects.GetTaskCreationProject(req.ProjectId!.Value, workspaceId.Value, cancellationToken);

        if (project is null)
        {
            return ClientResponse<TaskViewModel>.Failed($"Project with Id {req.ProjectId} not found");
        }

        await UnitOfWork.Statuses.EnsureNewTaskStatus(workspaceId.Value, user.Id, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var status = await ResolveStatus(
            req,
            project,
            workspaceId.Value,
            cancellationToken);

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
            ProjectTaskAppUsers = [new() { UserId = userId }],
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

        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.CompleteAsync(cancellationToken);

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
                await EventRecords.Append(new EventWriteRequest<ScopeMemberChangedPayload>
                {
                    WorkspaceId = task.WorkspaceId,
                    EventKey = EventKeys.ScopeMemberChanged,
                    SubjectType = EventEntityTypes.From(EntityType.Sprint),
                    SubjectId = task.SprintId.Value.ToString(),
                    Payload = new ScopeMemberChangedPayload
                    {
                        Change = "added",
                        MemberType = EventEntityTypes.From(EntityType.Task),
                        MemberId = result.Id.ToString(),
                        EstimateType = task.EstimateType?.ToString(),
                        EstimateValue = task.EstimateValue,
                        StatusId = task.StatusId,
                        StatusCategory = status.Category.ToString(),
                    },
                    References =
                    [
                        new EventReferenceInput
                        {
                            Role = EventReferenceRoles.Member,
                            EntityType = EventEntityTypes.From(EntityType.Task),
                            EntityId = result.Id.ToString(),
                        },
                        new EventReferenceInput
                        {
                            Role = EventReferenceRoles.Scope,
                            EntityType = EventEntityTypes.From(EntityType.Project),
                            EntityId = task.ProjectId!.Value.ToString(),
                        },
                    ],
                }, cancellationToken);
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

        await EventPublisher.Dispatch(new SearchIndexEvent
        {
            Operation = SearchIndexOperation.Index,
            EntityType = "task",
            EntityIds = [result.Id],
            WorkspaceSlug = workspaceKey,
        });

        return ClientResponse<TaskViewModel>.Success(response!);
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

    private async Task<Status?> ResolveStatus(AddProjectTaskRequest request, TaskCreationProject project, int workspaceId, CancellationToken cancellationToken)
    {

        if (request.StatusId.HasValue)
        {
            var requestedStatus = await UnitOfWork.Statuses.GetInWorkspace(request.StatusId.Value, workspaceId, cancellationToken: cancellationToken);

            return requestedStatus;
        }

        if (project.DefaultStatusId.HasValue)
        {
            var status = await UnitOfWork.Statuses.GetInWorkspace(project.DefaultStatusId.Value, workspaceId, cancellationToken: cancellationToken);

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

        var firstStatus = await UnitOfWork.Statuses.GetFirstTaskStatus(workspaceId, cancellationToken);

        return firstStatus;
    }
}
