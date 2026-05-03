using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Services.Sprints.Commands;

public sealed record CreateSprintCommand(AddSprintRequest Request) : IRequest<ClientResponse<SprintViewModel>>;

public sealed class CreateSprintCommandHandler : IRequestHandler<CreateSprintCommand, ClientResponse<SprintViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public CreateSprintCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<SprintViewModel>> Handle(CreateSprintCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey, cancellationToken: cancellationToken);

        if (workspace is null) return ClientResponse<SprintViewModel>.NotFound;
        if (string.IsNullOrWhiteSpace(req.Name)) return ClientResponse<SprintViewModel>.Failed("Sprint name is required");
        if (req.EndDate < req.StartDate) return ClientResponse<SprintViewModel>.Failed("Sprint end date must be after start date");

        var project = await UnitOfWork.Projects.GetAsync(req.ProjectId, true, cancellationToken);

        if (project is null || project.WorkspaceId != workspace.Id || project.IsDeleted)
        {
            return ClientResponse<SprintViewModel>.Failed($"Project with id {req.ProjectId} not found");
        }

        var user = await Identity.GetCurrentUser();
        var sprint = new Sprint
        {
            Name = req.Name.Trim(),
            Goal = req.Goal,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            Status = SprintStatus.Planning,
            ProjectId = req.ProjectId,
            WorkspaceId = workspace.Id,
            OwnerId = user.Id,
            CreatedByUserId = user.Id,
        };

        await UnitOfWork.Sprints.AddAsync(sprint, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var result = await UnitOfWork.Sprints.GetSprintDetailAsync(workspaceKey, sprint.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = sprint.Id;
            options.EntityType = EntityType.Sprint;
            options.Type = ActivityType.Create;
        });

        return result is null
            ? ClientResponse<SprintViewModel>.Failed("Sprint was created but could not be loaded")
            : ClientResponse<SprintViewModel>.Success(result);
    }
}
