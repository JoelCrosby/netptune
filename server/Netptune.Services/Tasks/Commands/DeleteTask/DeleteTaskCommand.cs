using Mediator;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Tasks.Commands;

public sealed record DeleteTaskCommand(int Id) : IRequest<ClientResponse>;
