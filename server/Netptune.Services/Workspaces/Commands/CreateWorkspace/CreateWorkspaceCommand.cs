using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Workspace;

namespace Netptune.Services.Workspaces.Commands;

public sealed record CreateWorkspaceCommand(AddWorkspaceRequest Request) : IRequest<ClientResponse<WorkspaceViewModel>>;
