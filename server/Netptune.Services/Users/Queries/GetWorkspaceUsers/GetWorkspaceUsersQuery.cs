using Mediator;

using Netptune.Core.ViewModels.Users;

namespace Netptune.Services.Users.Queries.GetWorkspaceUsers;

public sealed record GetWorkspaceUsersQuery : IRequest<List<WorkspaceUserViewModel>>;
