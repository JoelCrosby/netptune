using Mediator;

using Netptune.Core.Responses.Common;

namespace Netptune.Services.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskCommand(int Id) : IRequest<ClientResponse>;
