using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Commands;

public sealed record DeleteWorkspaceAiConversationsCommand(IReadOnlyCollection<Guid> ConversationIds) : IRequest<ClientResponse>;

public sealed class DeleteWorkspaceAiConversationsCommandHandler
    : IRequestHandler<DeleteWorkspaceAiConversationsCommand, ClientResponse>
{
    private const string ConversationSubjectType = "ai_conversation";

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiCancellationRegistry Turns;
    private readonly IEventRecordWriter EventRecords;

    public DeleteWorkspaceAiConversationsCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiCancellationRegistry turns,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Turns = turns;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse> Handle(
        DeleteWorkspaceAiConversationsCommand command,
        CancellationToken cancellationToken)
    {
        var conversationIds = command.ConversationIds.Distinct().ToList();

        if (conversationIds.Count == 0)
        {
            return ClientResponse.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var conversations = await UnitOfWork.AiConversations.GetManyInWorkspace(
            conversationIds,
            workspaceId,
            cancellationToken);

        if (conversations.Count == 0)
        {
            return ClientResponse.NotFound;
        }

        foreach (var conversation in conversations)
        {
            conversation.Delete(userId);

            var deletionEvent = BuildDeletionEvent(conversation);

            await EventRecords.Append(deletionEvent, cancellationToken);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        foreach (var conversation in conversations)
        {
            Turns.Stop(conversation.Id);
        }

        return ClientResponse.Success;
    }

    // The subject type is deliberately not an EntityType, so the deletion reaches the audit log
    // without projecting an entry into the workspace activity feed.
    private static EventWriteRequest<AssistantConversationDeletedPayload> BuildDeletionEvent(AiConversation conversation)
    {
        return new EventWriteRequest<AssistantConversationDeletedPayload>
        {
            WorkspaceId = conversation.WorkspaceId,
            EventKey = EventKeys.AssistantConversationDeleted,
            SubjectType = ConversationSubjectType,
            SubjectId = conversation.Id.ToString(),
            Payload = new AssistantConversationDeletedPayload
            {
                ConversationId = conversation.Id,
                Title = conversation.Title,
                OwnerUserId = conversation.UserId,
                OwnerDisplayName = conversation.User.DisplayName,
            },
        };
    }
}
