using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Core.Requests;

namespace Netptune.Services.Workspaces.Commands;

public sealed record CreateWorkspaceCommand(AddWorkspaceRequest Request) : IRequest<ClientResponse<WorkspaceViewModel>>;

public sealed class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, ClientResponse<WorkspaceViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public CreateWorkspaceCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<WorkspaceViewModel>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var user = await Identity.GetCurrentUser();
        var result = await WorkspaceFactory.CreateAsync(request.Request, user, UnitOfWork);

        if (result.IsSuccess && result.Payload is { } workspace)
        {
            Activity.Log(options =>
            {
                options.EntityId = workspace.Id;
                options.WorkspaceId = workspace.Id;
                options.EntityType = EntityType.Workspace;
                options.Type = ActivityType.Create;
            });
        }

        return result;
    }
}
