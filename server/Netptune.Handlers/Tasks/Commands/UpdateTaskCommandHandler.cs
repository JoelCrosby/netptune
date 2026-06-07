using Mediator;
using Microsoft.Extensions.Logging;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record UpdateTaskCommand(UpdateProjectTaskRequest Request) : IRequest<ClientResponse<TaskViewModel>>;

public sealed class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, ClientResponse<TaskViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;
    private readonly ILogger<UpdateTaskCommandHandler> Logger;
    private readonly IEventPublisher EventPublisher;
    private readonly IIdentityService Identity;

    public UpdateTaskCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IActivityLogger activity,
        ILogger<UpdateTaskCommandHandler> logger,
        IEventPublisher eventPublisher,
        IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
        Logger = logger;
        EventPublisher = eventPublisher;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<TaskViewModel>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var old = await UnitOfWork.Tasks.GetTaskViewModel(req.Id, cancellationToken);

        if (old is null) return ClientResponse<TaskViewModel>.NotFound;

        var result = await UnitOfWork.Tasks.GetTaskForUpdate(req.Id, cancellationToken);

        if (result is null) return ClientResponse<TaskViewModel>.NotFound;

        await UnitOfWork.Transaction(async () =>
        {
            if (req.Status.HasValue && result.Status != req.Status.Value)
            {
                await PutTaskInBoardGroup(req.Status.Value, result, cancellationToken);
            }

            result.Name = req.Name ?? result.Name;
            result.Description = req.Description ?? result.Description;
            result.Status = req.Status ?? result.Status;
            result.OwnerId = req.OwnerId ?? result.OwnerId;
            result.Priority = req.Priority ?? result.Priority;
            result.EstimateType = req.EstimateType ?? result.EstimateType;
            result.EstimateValue = req.EstimateValue ?? result.EstimateValue;

            if (req.AssigneeIds is not null)
            {
                result.ProjectTaskAppUsers = ProjectTaskAppUser.MergeUsersIds(
                    result.Id,
                    result.ProjectTaskAppUsers,
                    req.AssigneeIds).ToList();
            }

            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        var response = await UnitOfWork.Tasks.GetTaskViewModel(result.Id, cancellationToken);

        if (response is null) return ClientResponse<TaskViewModel>.NotFound;

        ProjectTaskDiff.Create(old, response).LogDiff(Activity, response.Id);

        if (old.Status != response.Status && response.WorkspaceId is not null)
        {
            await PublishStatusChanged(old, response);
        }

        return ClientResponse<TaskViewModel>.Success(response);
    }

    private Task PublishStatusChanged(TaskViewModel old, TaskViewModel current)
    {
        return EventPublisher.Dispatch(new TaskStatusChangedMessage
        {
            WorkspaceId = current.WorkspaceId!.Value,
            TaskId = current.Id,
            ActorUserId = Identity.GetCurrentUserId(),
            OldStatus = old.Status,
            NewStatus = current.Status,
        });
    }

    private async Task PutTaskInBoardGroup(ProjectTaskStatus status, Core.Entities.ProjectTask result, CancellationToken cancellationToken)
    {
        if (result.ProjectId is null) return;

        var groupType = status.GetGroupTypeFromTaskStatus();
        var group = await UnitOfWork.BoardGroups.GetDefaultTaskTarget(result.ProjectId.Value, groupType, cancellationToken);

        if (group is null)
        {
            Logger.LogInformation("Project With Id {ProjectId} does not have a default board group of type {GroupType}", result.ProjectId.Value, groupType);
            return;
        }

        await UnitOfWork.ProjectTasksInGroups.DeleteAllByTaskId(new[] { result.Id }, cancellationToken);

        await UnitOfWork.ProjectTasksInGroups.AddAsync(new ProjectTaskInBoardGroup
        {
            BoardGroupId = group.Id,
            ProjectTaskId = result.Id,
            SortOrder = group.MaxSortOrder + 1,
        }, cancellationToken);
    }
}
