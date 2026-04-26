using Mediator;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Users.Commands;

public sealed record RemoveUsersFromWorkspaceCommand(IEnumerable<string> Emails) : IRequest<ClientResponse<RemoveUsersWorkspaceResponse>>;
