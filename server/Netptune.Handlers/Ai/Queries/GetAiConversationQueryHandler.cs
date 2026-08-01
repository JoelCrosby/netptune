using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetAiConversationQuery(Guid ConversationId) : IRequest<ClientResponse<AiConversationDetailViewModel>>;

public sealed class GetAiConversationQueryHandler
    : IRequestHandler<GetAiConversationQuery, ClientResponse<AiConversationDetailViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAiConversationQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AiConversationDetailViewModel>> Handle(
        GetAiConversationQuery query,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var conversation = await UnitOfWork.AiConversations.GetOwned(
            query.ConversationId,
            userId,
            workspaceId,
            cancellationToken);

        if (conversation is null)
        {
            return ClientResponse<AiConversationDetailViewModel>.NotFound;
        }

        var messages = await UnitOfWork.AiConversations.GetMessages(conversation.Id, cancellationToken);
        var invocations = await UnitOfWork.AiConversations.GetToolInvocations(conversation.Id, cancellationToken);
        var referencesByMessage = AiMessageReferences.Group(invocations);
        var pendingChangeSet = await ReadPendingChangeSet(conversation.Id, userId, workspaceId, cancellationToken);
        var detail = new AiConversationDetailViewModel
        {
            Conversation = GetAiConversationsQueryHandler.ToViewModel(conversation, messages),
            Messages = messages.Select(message => ToViewModel(message, referencesByMessage)).ToList(),
            PendingChangeSet = pendingChangeSet,
        };

        return ClientResponse<AiConversationDetailViewModel>.Success(detail);
    }

    private async Task<AiChangeSetViewModel?> ReadPendingChangeSet(
        Guid conversationId,
        string userId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var changeSet = await UnitOfWork.AiChangeSets.GetPending(
            conversationId,
            userId,
            workspaceId,
            cancellationToken);

        if (changeSet is null)
        {
            return null;
        }

        var changes = await UnitOfWork.AiChangeSets.GetChanges(changeSet.Id, cancellationToken);

        return await AiChangeSetMapper.ToViewModel(changeSet, changes, UnitOfWork.Tasks, cancellationToken);
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
