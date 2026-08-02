using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Commands;

public sealed record StopAiTurnCommand(Guid ConversationId) : IRequest<ClientResponse>;

public sealed class StopAiTurnCommandHandler : IRequestHandler<StopAiTurnCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiTurnRegistry Turns;

    public StopAiTurnCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiTurnRegistry turns)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Turns = turns;
    }

    public async ValueTask<ClientResponse> Handle(
        StopAiTurnCommand command,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var conversation = await UnitOfWork.AiConversations.GetOwned(
            command.ConversationId,
            userId,
            workspaceId,
            cancellationToken);

        if (conversation is null)
        {
            return ClientResponse.NotFound;
        }

        Turns.Stop(command.ConversationId);

        return ClientResponse.Success;
    }
}
