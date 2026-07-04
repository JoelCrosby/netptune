using Mediator;
using Netptune.Core.Entities;
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
    private readonly IEventPublisher EventPublisher;
    private readonly IIdentityService Identity;

    public UpdateTaskCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IActivityLogger activity,
        IEventPublisher eventPublisher,
        IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
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
            var status = await ResolveStatus(req, result.WorkspaceId, cancellationToken);

            if (status is not null && result.StatusId != status.Id)
            {
                result.StatusId = status.Id;
            }

            result.Name = req.Name ?? result.Name;
            result.Description = req.Description ?? result.Description;
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

        var diff = ProjectTaskDiff.Create(old, response);

        diff.LogDiff(Activity, response.Id);

        if (diff.HasChanges && response.WorkspaceId is not null)
        {
            await PublishTaskChanged(response, diff);
        }

        return ClientResponse<TaskViewModel>.Success(response);
    }

    private Task PublishTaskChanged(TaskViewModel current, ProjectTaskDiff diff)
    {
        return EventPublisher.Dispatch(new TaskChangedMessage
        {
            WorkspaceId = current.WorkspaceId!.Value,
            TaskId = current.Id,
            ActorUserId = Identity.GetCurrentUserId(),
            Changes = diff.ToTaskFieldChanges(),
        });
    }

    private async Task<Status?> ResolveStatus(UpdateProjectTaskRequest request, int workspaceId, CancellationToken cancellationToken)
    {
        if (request.StatusId.HasValue)
        {
            return await UnitOfWork.Statuses.GetInWorkspace(request.StatusId.Value, workspaceId, cancellationToken: cancellationToken);
        }

        return null;
    }
}
