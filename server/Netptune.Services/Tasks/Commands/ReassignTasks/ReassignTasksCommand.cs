using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Tasks.Commands.ReassignTasks;

public sealed record ReassignTasksCommand(ReassignTasksRequest Request) : IRequest<ClientResponse>;
