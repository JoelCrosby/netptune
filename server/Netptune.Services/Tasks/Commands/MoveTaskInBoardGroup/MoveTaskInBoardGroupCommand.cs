using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Tasks.Commands;

public sealed record MoveTaskInBoardGroupCommand(MoveTaskInGroupRequest Request) : IRequest<ClientResponse>;
