using Mediator;

using Netptune.Core.Entities;

namespace Netptune.Services.Workspaces.Queries.GetWorkspace;

public sealed record GetWorkspaceQuery(string Slug) : IRequest<Workspace?>;
