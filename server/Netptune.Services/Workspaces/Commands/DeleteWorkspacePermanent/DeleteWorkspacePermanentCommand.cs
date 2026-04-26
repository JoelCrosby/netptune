using Mediator;

using Netptune.Core.Responses.Common;

namespace Netptune.Services.Workspaces.Commands.DeleteWorkspacePermanent;

public sealed record DeleteWorkspacePermanentCommand(string Key) : IRequest<ClientResponse>;
