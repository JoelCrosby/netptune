using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetWorkspaceAiConversationQuery(Guid ConversationId)
    : IRequest<ClientResponse<AiConversationDetailViewModel>>;

public sealed class GetWorkspaceAiConversationQueryHandler
    : IRequestHandler<GetWorkspaceAiConversationQuery, ClientResponse<AiConversationDetailViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetWorkspaceAiConversationQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AiConversationDetailViewModel>> Handle(
        GetWorkspaceAiConversationQuery query,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var conversation = await UnitOfWork.AiConversations.GetInWorkspace(
            query.ConversationId,
            workspaceId,
            cancellationToken);

        if (conversation is null)
        {
            return ClientResponse<AiConversationDetailViewModel>.NotFound;
        }

        var messages = await UnitOfWork.AiConversations.GetMessages(conversation.Id, cancellationToken);
        var invocations = await UnitOfWork.AiConversations.GetToolInvocations(conversation.Id, cancellationToken);
        var referencesByMessage = AiMessageReferences.Group(invocations);
        var detail = new AiConversationDetailViewModel
        {
            Conversation = GetAiConversationsQueryHandler.ToViewModel(conversation, messages),
            Messages = AiMessageMapper.ToViewModels(messages, referencesByMessage, []),
        };

        return ClientResponse<AiConversationDetailViewModel>.Success(detail);
    }
}
