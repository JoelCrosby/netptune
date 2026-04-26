using Mediator;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Users.Commands;

public sealed record InviteUsersToWorkspaceCommand(IEnumerable<string> Emails) : IRequest<ClientResponse<InviteUserResponse>>;
