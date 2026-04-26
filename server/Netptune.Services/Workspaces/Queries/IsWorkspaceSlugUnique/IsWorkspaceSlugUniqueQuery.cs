using Mediator;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Workspaces.Queries;

public sealed record IsWorkspaceSlugUniqueQuery(string Slug) : IRequest<ClientResponse<IsSlugUniqueResponse>>;
