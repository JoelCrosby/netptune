using Mediator;

using Netptune.Core.Entities;
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
            Messages = messages.Select(message => ToViewModel(message, referencesByMessage)).ToList(),
        };

        return ClientResponse<AiConversationDetailViewModel>.Success(detail);
    }

    private static AiMessageViewModel ToViewModel(
        AiMessage message,
        IReadOnlyDictionary<long, List<AiEntityReference>> referencesByMessage)
    {
        var content = AiMessageContent.FromJsonDocument(message.Content);
        var hasReferences = referencesByMessage.TryGetValue(message.Id, out var references);

        return new AiMessageViewModel
        {
            Id = message.Id,
            Sequence = message.Sequence,
            Role = message.Role,
            Text = content.Text,
            ToolNames = content.ToolCalls.Select(call => call.Name).ToList(),
            References = hasReferences ? references! : [],
            CreatedAt = message.CreatedAt,
        };
    }
}
