using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Core.Models.Options;
using Microsoft.Extensions.Options;

namespace Netptune.Handlers.Workspaces.Commands;

public sealed record CreateWorkspaceForNewUserCommand(AddWorkspaceRequest Request, AppUser User) : IRequest<ClientResponse<WorkspaceViewModel>>;

public sealed class CreateWorkspaceForNewUserCommandHandler : IRequestHandler<CreateWorkspaceForNewUserCommand, ClientResponse<WorkspaceViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly WorkspaceStorageOptions StorageOptions;

    public CreateWorkspaceForNewUserCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IOptions<WorkspaceStorageOptions> storageOptions)
    {
        UnitOfWork = unitOfWork;
        StorageOptions = storageOptions.Value;
    }

    public async ValueTask<ClientResponse<WorkspaceViewModel>> Handle(CreateWorkspaceForNewUserCommand request, CancellationToken cancellationToken)
    {
        return await WorkspaceFactory.CreateAsync(
            request.Request,
            request.User,
            UnitOfWork,
            StorageOptions.DefaultWorkspaceLimitBytes,
            cancellationToken);
    }
}
