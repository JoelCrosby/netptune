using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Services.Sprints.Commands;

public sealed record CompleteSprintCommand(int Id) : IRequest<ClientResponse<SprintViewModel>>;

public sealed class CompleteSprintCommandHandler : IRequestHandler<CompleteSprintCommand, ClientResponse<SprintViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public CompleteSprintCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<SprintViewModel>> Handle(CompleteSprintCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintInWorkspaceAsync(workspaceKey, request.Id, cancellationToken: cancellationToken);

        if (sprint is null) return ClientResponse<SprintViewModel>.NotFound;
        if (sprint.Status != SprintStatus.Active) return ClientResponse<SprintViewModel>.Failed("Only active sprints can be completed");

        var user = await Identity.GetCurrentUser();
        sprint.Status = SprintStatus.Completed;
        sprint.CompletedAt = DateTime.UtcNow;
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
