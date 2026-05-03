using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Services.Sprints.Commands;

public sealed record UpdateSprintCommand(UpdateSprintRequest Request) : IRequest<ClientResponse<SprintViewModel>>;

public sealed class UpdateSprintCommandHandler : IRequestHandler<UpdateSprintCommand, ClientResponse<SprintViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public UpdateSprintCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<SprintViewModel>> Handle(UpdateSprintCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintInWorkspaceAsync(workspaceKey, req.Id, cancellationToken: cancellationToken);

        if (sprint is null) return ClientResponse<SprintViewModel>.NotFound;
        if (sprint.Status == SprintStatus.Completed && req.Status != SprintStatus.Cancelled)
        {
            return ClientResponse<SprintViewModel>.Failed("Completed sprints cannot be edited");
        }

        if (!string.IsNullOrWhiteSpace(req.Name)) sprint.Name = req.Name.Trim();
        sprint.Goal = req.Goal ?? sprint.Goal;
        sprint.StartDate = req.StartDate ?? sprint.StartDate;
        sprint.EndDate = req.EndDate ?? sprint.EndDate;

        if (sprint.EndDate < sprint.StartDate)
        {
            return ClientResponse<SprintViewModel>.Failed("Sprint end date must be after start date");
        }

        if (req.Status.HasValue)
        {
            sprint.Status = req.Status.Value;
            sprint.CompletedAt = req.Status.Value == SprintStatus.Completed
                ? DateTime.UtcNow
                : sprint.CompletedAt;
        }

        var user = await Identity.GetCurrentUser();
        sprint.ModifiedByUserId = user.Id;

        await UnitOfWork.CompleteAsync(cancellationToken);

        var result = await UnitOfWork.Sprints.GetSprintDetailAsync(workspaceKey, sprint.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = sprint.Id;
            options.EntityType = EntityType.Sprint;
            options.Type = ActivityType.Modify;
        });

        return result is null
            ? ClientResponse<SprintViewModel>.NotFound
            : ClientResponse<SprintViewModel>.Success(result);
    }
}
