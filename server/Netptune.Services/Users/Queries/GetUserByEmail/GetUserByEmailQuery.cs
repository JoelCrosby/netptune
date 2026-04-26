using Mediator;

using Netptune.Core.ViewModels.Users;

namespace Netptune.Services.Users.Queries.GetUserByEmail;

public sealed record GetUserByEmailQuery(string Email) : IRequest<UserViewModel?>;
