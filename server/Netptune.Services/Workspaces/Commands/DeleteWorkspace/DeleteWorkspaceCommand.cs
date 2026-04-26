using Mediator;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Workspaces.Commands;

public sealed record DeleteWorkspaceCommand(string Key) : IRequest<ClientResponse>;
