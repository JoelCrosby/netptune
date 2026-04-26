using Mediator;

using Netptune.Core.Responses.Common;

namespace Netptune.Services.Notifications.Commands.MarkAsRead;

public sealed record MarkAsReadCommand(int Id) : IRequest<ClientResponse>;
