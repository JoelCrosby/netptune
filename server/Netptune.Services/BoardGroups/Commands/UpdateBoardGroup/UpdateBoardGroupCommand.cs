using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.BoardGroups.Commands.UpdateBoardGroup;

public sealed record UpdateBoardGroupCommand(UpdateBoardGroupRequest Request) : IRequest<ClientResponse<BoardGroupViewModel>>;
