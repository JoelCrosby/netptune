using Mediator;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Boards.Commands;

public sealed record DeleteBoardCommand(int Id) : IRequest<ClientResponse>;
