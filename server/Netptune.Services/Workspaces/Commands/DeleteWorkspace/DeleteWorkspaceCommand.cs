using Mediator;

using Netptune.Core.Responses.Common;

namespace Netptune.Services.Workspaces.Commands.DeleteWorkspace;

public sealed record DeleteWorkspaceCommand(string Key) : IRequest<ClientResponse>;
