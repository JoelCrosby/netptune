using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Services.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(UpdateUserRequest Request) : IRequest<ClientResponse<UserViewModel>>;
