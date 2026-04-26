using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Users.Commands.ToggleUserPermission;

public sealed record ToggleUserPermissionCommand(ToggleUserPermissionRequest Request) : IRequest<ClientResponse<List<string>>>;
