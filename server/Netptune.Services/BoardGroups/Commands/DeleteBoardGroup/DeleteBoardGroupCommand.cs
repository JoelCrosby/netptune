using Mediator;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.BoardGroups.Commands;

public sealed record DeleteBoardGroupCommand(int Id) : IRequest<ClientResponse>;
