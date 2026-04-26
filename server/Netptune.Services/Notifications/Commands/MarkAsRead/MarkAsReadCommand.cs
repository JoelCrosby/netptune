using Mediator;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Notifications.Commands;

public sealed record MarkAsReadCommand(int Id) : IRequest<ClientResponse>;
