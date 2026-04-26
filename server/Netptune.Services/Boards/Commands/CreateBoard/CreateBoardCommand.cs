using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Commands.CreateBoard;

public sealed record CreateBoardCommand(AddBoardRequest Request) : IRequest<ClientResponse<BoardViewModel>>;
