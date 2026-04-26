using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.BoardGroups.Commands.CreateBoardGroup;

public sealed record CreateBoardGroupCommand(AddBoardGroupRequest Request) : IRequest<ClientResponse<BoardGroupViewModel>>;
