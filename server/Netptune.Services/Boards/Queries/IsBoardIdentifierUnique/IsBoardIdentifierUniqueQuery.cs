using Mediator;

using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Boards.Queries.IsBoardIdentifierUnique;

public sealed record IsBoardIdentifierUniqueQuery(string Identifier) : IRequest<ClientResponse<IsSlugUniqueResponse>>;
