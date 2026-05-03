using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Services.Sprints.Commands;

public sealed record RemoveTaskFromSprintCommand(int SprintId, int TaskId) : IRequest<ClientResponse<SprintDetailViewModel>>;

public sealed class RemoveTaskFromSprintCommandHandler : IRequestHandler<RemoveTaskFromSprintCommand, ClientResponse<SprintDetailViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public RemoveTaskFromSprintCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<SprintDetailViewModel>> Handle(RemoveTaskFromSprintCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintInWorkspaceAsync(workspaceKey, request.SprintId, cancellationToken: cancellationToken);

        if (sprint is null) return ClientResponse<SprintDetailViewModel>.NotFound;
        if (sprint.Status == SprintStatus.Completed) return ClientResponse<SprintDetailViewModel>.Failed("Completed sprints cannot be changed");

        var task = await UnitOfWork.Tasks.GetAsync(request.TaskId, cancellationToken: cancellationToken);

        if (task is null || task.SprintId != sprint.Id)
        {
            return ClientResponse<SprintDetailViewModel>.NotFound;
        }

        task.SprintId = null;

        await UnitOfWork.CompleteAsync(cancellationToken);

        var result = await UnitOfWork.Sprints.GetSprintDetailAsync(workspaceKey, sprint.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = sprint.Id;
            options.EntityType = EntityType.Sprint;
            options.Type = ActivityType.Unassign;
        });

        return result is null
            ? ClientResponse<SprintDetailViewModel>.NotFound
            : ClientResponse<SprintDetailViewModel>.Success(result);
    }
}
