using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Workspace;

namespace Netptune.Services.Workspaces.Commands.CreateWorkspaceForNewUser;

public sealed record CreateWorkspaceForNewUserCommand(AddWorkspaceRequest Request, AppUser User) : IRequest<ClientResponse<WorkspaceViewModel>>;
