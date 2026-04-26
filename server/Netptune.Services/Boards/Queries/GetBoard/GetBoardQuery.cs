using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Queries;

public sealed record GetBoardQuery(int Id) : IRequest<ClientResponse<BoardViewModel>>;
