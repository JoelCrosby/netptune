using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Services.Sprints.Commands;

public sealed record AddTasksToSprintCommand(int SprintId, AddTasksToSprintRequest Request) : IRequest<ClientResponse<SprintDetailViewModel>>;

public sealed class AddTasksToSprintCommandHandler : IRequestHandler<AddTasksToSprintCommand, ClientResponse<SprintDetailViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public AddTasksToSprintCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<SprintDetailViewModel>> Handle(AddTasksToSprintCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintInWorkspaceAsync(workspaceKey, request.SprintId, cancellationToken: cancellationToken);

        if (sprint is null) return ClientResponse<SprintDetailViewModel>.NotFound;
        if (sprint.Status == SprintStatus.Completed) return ClientResponse<SprintDetailViewModel>.Failed("Completed sprints cannot be changed");
        if (request.Request.TaskIds.Count == 0) return ClientResponse<SprintDetailViewModel>.Failed("At least one task is required");

        var taskIds = request.Request.TaskIds.Distinct().ToList();

        foreach (var taskId in taskIds)
        {
            var task = await UnitOfWork.Tasks.GetAsync(taskId, cancellationToken: cancellationToken);

            if (task is null || task.IsDeleted || task.WorkspaceId != sprint.WorkspaceId)
            {
                return ClientResponse<SprintDetailViewModel>.Failed($"Task with id {taskId} not found");
            }

            if (task.ProjectId != sprint.ProjectId)
            {
                return ClientResponse<SprintDetailViewModel>.Failed($"Task with id {taskId} is not in sprint project");
            }

            task.SprintId = sprint.Id;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

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
