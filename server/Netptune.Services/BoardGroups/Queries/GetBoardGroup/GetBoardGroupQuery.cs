using Mediator;

using Netptune.Core.Entities;

namespace Netptune.Services.BoardGroups.Queries.GetBoardGroup;

public sealed record GetBoardGroupQuery(int Id) : IRequest<BoardGroup?>;
