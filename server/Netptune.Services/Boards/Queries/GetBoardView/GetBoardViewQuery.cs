using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Queries.GetBoardView;

public sealed record GetBoardViewQuery(string Identifier, BoardGroupsFilter? Filter) : IRequest<ClientResponse<BoardView>>;
