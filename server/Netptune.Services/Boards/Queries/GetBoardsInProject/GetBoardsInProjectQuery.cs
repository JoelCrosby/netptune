using Mediator;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Queries;

public sealed record GetBoardsInProjectQuery(int ProjectId) : IRequest<List<BoardViewModel>?>;
