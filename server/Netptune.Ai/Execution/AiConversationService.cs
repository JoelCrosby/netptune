using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

namespace Netptune.Ai.Execution;

public sealed class AiConversationService : IAiConversationService
{
    private const int MaximumTitleLength = 80;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiCredentialProtector Protector;
    private readonly IAiConversationRunner Runner;
    private readonly IAiChatProviderFactory ProviderFactory;
    private readonly IAiSystemPromptBuilder PromptBuilder;
    private readonly AiOptions Options;

    public AiConversationService(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiCredentialProtector protector,
        IAiConversationRunner runner,
        IAiChatProviderFactory providerFactory,
        IAiSystemPromptBuilder promptBuilder,
        IOptions<AiOptions> options)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Protector = protector;
        Runner = runner;
        ProviderFactory = providerFactory;
        PromptBuilder = promptBuilder;
        Options = options.Value;
    }

    public async IAsyncEnumerable<AiStreamEvent> SendMessage(
        AiSendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var text = request.Text.Trim();
        var hasText = text.Length > 0;

        if (!hasText)
        {
            yield return AiStreamEvent.Failed("A message is required.");

            yield break;
        }

        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var workspaceKey = Identity.GetWorkspaceKey();
        var credential = await UnitOfWork.AiCredentials.GetForProvider(userId, AiProvider.Anthropic, cancellationToken);

        if (credential is null)
        {
            yield return AiStreamEvent.Failed("No API key is configured for the assistant.");

            yield break;
        }

        var conversation = await ResolveConversation(request.ConversationId, userId, workspaceId, text, cancellationToken);

        if (conversation is null)
        {
            yield return AiStreamEvent.Failed("Conversation not found.");

            yield break;
        }

        yield return AiStreamEvent.ConversationStarted(conversation.Id);

        var membership = await UnitOfWork.WorkspaceUsers.GetUserPermissions(
            userId,
            workspaceKey,
            cancellationToken: cancellationToken);

        if (membership is null)
        {
            yield return AiStreamEvent.Failed("You are not a member of this workspace.");

            yield break;
        }

        var history = await LoadHistory(conversation.Id, cancellationToken);
        var userMessage = new AiChatMessage { Role = AiMessageRole.User, Text = text };

        await PersistMessage(conversation, userMessage, AiMessageRole.User, null, cancellationToken);

        history.Add(userMessage);

        var apiKey = Protector.Unprotect(credential.Secret);
        var systemPrompt = await PromptBuilder.Build(cancellationToken);
        var context = new AiRunContext
        {
            Provider = conversation.Provider,
            Model = conversation.Model,
            ApiKey = apiKey,
            SystemPrompt = systemPrompt,
            History = history,
            Permissions = membership.Permissions.ToHashSet(StringComparer.Ordinal),
        };

        var assistantText = new StringBuilder();

        await foreach (var streamEvent in Runner.Run(context, cancellationToken))
        {
            if (streamEvent.Type == AiStreamEventType.TextDelta && streamEvent.Text is not null)
            {
                assistantText.Append(streamEvent.Text);
            }

            yield return streamEvent;
        }

        await PersistTurn(conversation, context, assistantText.ToString(), cancellationToken);

        credential.LastUsedAt = DateTime.UtcNow;

        await UnitOfWork.CompleteAsync(cancellationToken);
    }

    private async Task<AiConversation?> ResolveConversation(
        Guid? conversationId,
        string userId,
        int workspaceId,
        string firstMessage,
        CancellationToken cancellationToken)
    {
        if (conversationId.HasValue)
        {
            return await UnitOfWork.AiConversations.GetOwned(
                conversationId.Value,
                userId,
                workspaceId,
                cancellationToken);
        }

        var provider = AiProvider.Anthropic;
        var model = ProviderFactory.Resolve(provider).DefaultModel;
        var conversation = new AiConversation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = userId,
            Title = CreateTitle(firstMessage),
            Provider = provider,
            Model = model,
            LastMessageAt = DateTime.UtcNow,
        };

        var created = await UnitOfWork.AiConversations.AddAsync(conversation, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        return created;
    }

    private static string CreateTitle(string firstMessage)
    {
        var normalised = firstMessage.ReplaceLineEndings(" ").Trim();
        var isShort = normalised.Length <= MaximumTitleLength;

        if (isShort)
        {
            return normalised;
        }

        return $"{normalised[..MaximumTitleLength].TrimEnd()}…";
    }

    private async Task<List<AiChatMessage>> LoadHistory(Guid conversationId, CancellationToken cancellationToken)
    {
        var messages = await UnitOfWork.AiConversations.GetMessages(conversationId, cancellationToken);

        return messages
            .Select(message => AiMessageContent.FromJsonDocument(message.Content).ToChatMessage(message.Role))
            .ToList();
    }

    private async Task PersistMessage(
        AiConversation conversation,
        AiChatMessage message,
        AiMessageRole role,
        AiChatTurn? turn,
        CancellationToken cancellationToken)
    {
        var sequence = await UnitOfWork.AiConversations.GetNextSequence(conversation.Id, cancellationToken);
        var content = AiMessageContent.FromChatMessage(message);
        var usage = turn?.Usage ?? new AiUsage();
        var record = new AiMessage
        {
            ConversationId = conversation.Id,
            Sequence = sequence,
            Role = role,
            Content = content.ToJsonDocument(),
            ProviderPayload = turn?.ProviderPayload,
            Provider = conversation.Provider,
            Model = conversation.Model,
            Status = AiMessageStatus.Complete,
            FinishReason = turn?.FinishReason,
            InputTokens = usage.InputTokens,
            OutputTokens = usage.OutputTokens,
            CacheReadTokens = usage.CacheReadTokens,
            CreatedAt = DateTime.UtcNow,
        };

        await UnitOfWork.AiConversations.AddMessage(record, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        conversation.MessageCount += 1;
        conversation.LastMessageAt = record.CreatedAt;
    }

    private async Task PersistTurn(
        AiConversation conversation,
        AiRunContext context,
        string assistantText,
        CancellationToken cancellationToken)
    {
        var lastTurn = context.Turns.LastOrDefault();
        var assistantMessage = new AiChatMessage
        {
            Role = AiMessageRole.Assistant,
            Text = assistantText,
            ToolCalls = lastTurn?.ToolCalls ?? [],
        };

        await PersistMessage(conversation, assistantMessage, AiMessageRole.Assistant, lastTurn, cancellationToken);

        var hasInvocations = context.Invocations.Count > 0;

        if (!hasInvocations)
        {
            return;
        }

        var messages = await UnitOfWork.AiConversations.GetMessages(conversation.Id, cancellationToken);
        var assistantRecord = messages.LastOrDefault(message => message.Role == AiMessageRole.Assistant);

        if (assistantRecord is null)
        {
            return;
        }

        var invocations = context.Invocations.Select(invocation => new AiToolInvocation
        {
            ConversationId = conversation.Id,
            MessageId = assistantRecord.Id,
            ToolName = invocation.ToolName,
            Arguments = invocation.Arguments,
            Result = JsonDocument.Parse(JsonSerializer.Serialize(invocation.Result)),
            ResultTruncated = invocation.Truncated,
            Status = invocation.IsError ? AiToolInvocationStatus.Failed : AiToolInvocationStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
        });

        await UnitOfWork.AiConversations.AddToolInvocations(invocations, cancellationToken);
    }
}
