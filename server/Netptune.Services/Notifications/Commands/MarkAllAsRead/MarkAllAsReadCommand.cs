using Mediator;

using Netptune.Core.Responses.Common;

namespace Netptune.Services.Notifications.Commands.MarkAllAsRead;

public sealed record MarkAllAsReadCommand : IRequest<ClientResponse>;
