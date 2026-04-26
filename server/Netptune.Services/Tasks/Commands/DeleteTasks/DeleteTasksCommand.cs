using Mediator;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Tasks.Commands;

public sealed record DeleteTasksCommand(IEnumerable<int> Ids) : IRequest<ClientResponse>;
