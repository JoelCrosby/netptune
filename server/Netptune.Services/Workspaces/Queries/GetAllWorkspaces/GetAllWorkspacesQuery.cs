using Mediator;

using Netptune.Core.Entities;

namespace Netptune.Services.Workspaces.Queries.GetAllWorkspaces;

public sealed record GetAllWorkspacesQuery : IRequest<List<Workspace>>;
