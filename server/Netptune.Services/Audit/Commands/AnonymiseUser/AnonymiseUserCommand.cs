using Mediator;

using Netptune.Core.Responses.Common;

namespace Netptune.Services.Audit.Commands.AnonymiseUser;

public sealed record AnonymiseUserCommand(string UserId) : IRequest<ClientResponse>;
