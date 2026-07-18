using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Handlers.Sprints.Commands;

public sealed record AddTasksToSprintCommand(int SprintId, AddTasksToSprintRequest Request) : IRequest<ClientResponse<SprintDetailViewModel>>;

public sealed class AddTasksToSprintCommandHandler : IRequestHandler<AddTasksToSprintCommand, ClientResponse<SprintDetailViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventRecordWriter EventRecords;

    public AddTasksToSprintCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse<SprintDetailViewModel>> Handle(AddTasksToSprintCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetTaskAssignmentTarget(workspaceKey, request.SprintId, cancellationToken);

        if (sprint is null)
        {
            return ClientResponse<SprintDetailViewModel>.NotFound;
        }

        if (sprint.Status == SprintStatus.Completed)
        {
            return ClientResponse<SprintDetailViewModel>.Failed("Completed sprints cannot be changed");
        }

        if (request.Request.TaskIds.Count == 0)
        {
            return ClientResponse<SprintDetailViewModel>.Failed("At least one task is required");
        }

        var taskIds = request.Request.TaskIds.Distinct().ToList();
        var taskIdsInWorkspace = await UnitOfWork.Tasks.GetValidTaskIdsInWorkspace(taskIds, sprint.WorkspaceId, cancellationToken);

        foreach (var taskId in taskIds.Except(taskIdsInWorkspace))
        {
            return ClientResponse<SprintDetailViewModel>.Failed($"Task with id {taskId} not found");
        }

        var taskIdsInProject = await UnitOfWork.Tasks.GetValidTaskIdsInProject(
            taskIds,
            sprint.WorkspaceId,
            sprint.ProjectId,
            cancellationToken);

        foreach (var taskId in taskIds.Except(taskIdsInProject))
        {
            return ClientResponse<SprintDetailViewModel>.Failed($"Task with id {taskId} is not in sprint project");
        }

        var tasks = await UnitOfWork.Tasks.GetTaskViewModels(taskIds, cancellationToken);

        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.Tasks.AssignTasksToSprint(taskIds, sprint.Id, cancellationToken);

            foreach (var task in tasks.Where(_ => sprint.Status == SprintStatus.Active))
            {
                await EventRecords.Append(new EventWriteRequest<ScopeMemberChangedPayload>
                {
                    WorkspaceId = sprint.WorkspaceId,
                    EventKey = EventKeys.ScopeMemberChanged,
                    SubjectType = EventEntityTypes.From(EntityType.Sprint),
                    SubjectId = sprint.Id.ToString(),
                    Payload = new ScopeMemberChangedPayload
                    {
                        Change = "added",
                        MemberType = EventEntityTypes.From(EntityType.Task),
                        MemberId = task.Id.ToString(),
                        EstimateType = task.EstimateType?.ToString(),
                        EstimateValue = task.EstimateValue,
                        StatusId = task.StatusId,
                        StatusCategory = task.StatusCategory.ToString(),
                    },
                    References =
                    [
                        new EventReferenceInput(
                            EventReferenceRoles.Member,
                            EventEntityTypes.From(EntityType.Task),
                            task.Id.ToString()),
                        new EventReferenceInput(
                            EventReferenceRoles.Scope,
                            EventEntityTypes.From(EntityType.Project),
                            sprint.ProjectId.ToString()),
                    ],
                }, cancellationToken);
            }

            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        var result = await UnitOfWork.Sprints.GetSprintDetailAsync(workspaceKey, sprint.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = sprint.Id;
            options.EntityType = EntityType.Sprint;
            options.Type = ActivityType.Assign;
        });

        return result is null
            ? ClientResponse<SprintDetailViewModel>.NotFound
            : ClientResponse<SprintDetailViewModel>.Success(result);
    }
}
