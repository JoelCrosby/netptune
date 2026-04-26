using Mediator;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Services.Users.Queries;

public sealed record GetUserQuery(string UserId) : IRequest<UserViewModel?>;
