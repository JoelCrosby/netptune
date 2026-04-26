using Mediator;

using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Queries.GetBoardsInProject;

public sealed record GetBoardsInProjectQuery(int ProjectId) : IRequest<List<BoardViewModel>?>;
