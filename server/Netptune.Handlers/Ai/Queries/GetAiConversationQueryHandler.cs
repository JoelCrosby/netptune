using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetAiConversationQuery(Guid ConversationId) : IRequest<ClientResponse<AiConversationDetailViewModel>>;

public sealed class GetAiConversationQueryHandler
    : IRequestHandler<GetAiConversationQuery, ClientResponse<AiConversationDetailViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiUndoCatalog UndoCatalog;

    public GetAiConversationQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiUndoCatalog undoCatalog)
    {
        UndoCatalog = undoCatalog;
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
        var changeSets = await UnitOfWork.AiChangeSets.GetForConversation(
            conversation.Id,
            userId,
            workspaceId,
            cancellationToken);

        var detail = new AiConversationDetailViewModel
        {
            Conversation = GetAiConversationsQueryHandler.ToViewModel(conversation, messages),
            Messages = AiMessageMapper.ToViewModels(messages, referencesByMessage, changeSets),
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
        var taskIds = AiChangeSetMapper.CollectTaskIds(changes);
        var tasks = await UnitOfWork.Tasks.GetTaskViewModels(taskIds, cancellationToken);

        return AiChangeSetMapper.ToViewModel(changeSet, changes, tasks, UndoCatalog);
    }
}
