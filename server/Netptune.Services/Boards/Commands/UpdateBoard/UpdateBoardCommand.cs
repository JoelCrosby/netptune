using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Commands;

public sealed record UpdateBoardCommand(UpdateBoardRequest Request) : IRequest<ClientResponse<BoardViewModel>>;
