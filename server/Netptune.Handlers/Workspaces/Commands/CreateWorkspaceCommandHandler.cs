using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Core.Models.Options;
using Microsoft.Extensions.Options;

namespace Netptune.Handlers.Workspaces.Commands;

public sealed record CreateWorkspaceCommand(AddWorkspaceRequest Request) : IRequest<ClientResponse<WorkspaceViewModel>>;

public sealed class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, ClientResponse<WorkspaceViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly WorkspaceStorageOptions StorageOptions;

    public CreateWorkspaceCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        IOptions<WorkspaceStorageOptions> storageOptions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        StorageOptions = storageOptions.Value;
    }

    public async ValueTask<ClientResponse<WorkspaceViewModel>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var user = await Identity.GetCurrentUser();

        var result = await WorkspaceFactory.CreateAsync(
            request.Request,
            user,
            UnitOfWork,
            StorageOptions.DefaultWorkspaceLimitBytes,
            cancellationToken);

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
