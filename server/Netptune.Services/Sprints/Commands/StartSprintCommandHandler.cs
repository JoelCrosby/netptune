using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Services.Sprints.Commands;

public sealed record StartSprintCommand(int Id) : IRequest<ClientResponse<SprintViewModel>>;

public sealed class StartSprintCommandHandler : IRequestHandler<StartSprintCommand, ClientResponse<SprintViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public StartSprintCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<SprintViewModel>> Handle(StartSprintCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintInWorkspaceAsync(workspaceKey, request.Id, cancellationToken: cancellationToken);

        if (sprint is null) return ClientResponse<SprintViewModel>.NotFound;
        if (sprint.Status != SprintStatus.Planning) return ClientResponse<SprintViewModel>.Failed("Only planning sprints can be started");

        var hasActiveSprint = await UnitOfWork.Sprints.HasActiveSprintAsync(sprint.ProjectId, sprint.Id, cancellationToken);

        if (hasActiveSprint) return ClientResponse<SprintViewModel>.Failed("This project already has an active sprint");

        var user = await Identity.GetCurrentUser();
        sprint.Status = SprintStatus.Active;
        sprint.ModifiedByUserId = user.Id;

        await UnitOfWork.CompleteAsync(cancellationToken);

        var result = await UnitOfWork.Sprints.GetSprintDetailAsync(workspaceKey, sprint.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = sprint.Id;
            options.EntityType = EntityType.Sprint;
            options.Type = ActivityType.ModifyStatus;
        });

        return result is null
            ? ClientResponse<SprintViewModel>.NotFound
            : ClientResponse<SprintViewModel>.Success(result);
    }
}
