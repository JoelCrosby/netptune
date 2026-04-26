using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Tasks.Commands;

public sealed record MoveTasksToGroupCommand(MoveTasksToGroupRequest Request) : IRequest<ClientResponse>;
