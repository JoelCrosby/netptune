using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Commands;

public sealed record DeleteAiConversationCommand(Guid ConversationId) : IRequest<ClientResponse>;

public sealed class DeleteAiConversationCommandHandler : IRequestHandler<DeleteAiConversationCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public DeleteAiConversationCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(
        DeleteAiConversationCommand command,
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

        conversation.Delete(userId);

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
