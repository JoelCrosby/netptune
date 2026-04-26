using Mediator;

using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Queries.GetBoardsInWorkspace;

public sealed record GetBoardsInWorkspaceQuery : IRequest<List<BoardsViewModel>?>;
