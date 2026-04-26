using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Core.Entities;
using Netptune.Core.Requests;

namespace Netptune.Services.Workspaces.Commands;

public sealed record CreateWorkspaceForNewUserCommand(AddWorkspaceRequest Request, AppUser User) : IRequest<ClientResponse<WorkspaceViewModel>>;

public sealed class CreateWorkspaceForNewUserCommandHandler : IRequestHandler<CreateWorkspaceForNewUserCommand, ClientResponse<WorkspaceViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public CreateWorkspaceForNewUserCommandHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async ValueTask<ClientResponse<WorkspaceViewModel>> Handle(CreateWorkspaceForNewUserCommand request, CancellationToken cancellationToken)
    {
        return await WorkspaceFactory.CreateAsync(request.Request, request.User, UnitOfWork);
    }
}
