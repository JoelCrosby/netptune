using Mediator;

using Netptune.Core.ViewModels.Users;

namespace Netptune.Services.Users.Queries.GetAllUsers;

public sealed record GetAllUsersQuery : IRequest<List<UserViewModel>>;
