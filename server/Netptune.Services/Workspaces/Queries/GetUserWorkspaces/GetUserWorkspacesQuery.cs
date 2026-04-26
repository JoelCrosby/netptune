using Mediator;

using Netptune.Core.Entities;

namespace Netptune.Services.Workspaces.Queries.GetUserWorkspaces;

public sealed record GetUserWorkspacesQuery : IRequest<List<Workspace>>;
