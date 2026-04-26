using Mediator;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Notifications.Queries;

public sealed record GetUnreadCountQuery : IRequest<ClientResponse<int>>;
