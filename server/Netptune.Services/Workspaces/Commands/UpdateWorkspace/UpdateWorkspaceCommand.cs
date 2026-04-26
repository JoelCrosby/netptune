using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Workspaces.Commands.UpdateWorkspace;

public sealed record UpdateWorkspaceCommand(UpdateWorkspaceRequest Request) : IRequest<ClientResponse<Workspace>>;
