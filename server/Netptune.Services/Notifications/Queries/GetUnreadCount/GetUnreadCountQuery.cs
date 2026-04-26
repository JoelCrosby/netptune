using Mediator;

using Netptune.Core.Responses.Common;

namespace Netptune.Services.Notifications.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery : IRequest<ClientResponse<int>>;
