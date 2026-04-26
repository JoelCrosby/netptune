using Mediator;
using Netptune.Core.Entities;

namespace Netptune.Services.Workspaces.Queries;

public sealed record GetAllWorkspacesQuery : IRequest<List<Workspace>>;
