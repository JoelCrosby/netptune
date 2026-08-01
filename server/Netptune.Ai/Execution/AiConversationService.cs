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

    private static readonly JsonSerializerOptions FieldSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiCredentialProtector Protector;
    private readonly IAiConversationRunner Runner;
    private readonly IAiChatProviderFactory ProviderFactory;
    private readonly IAiSystemPromptBuilder PromptBuilder;
    private readonly IAiChangeSetBuilder ChangeSetBuilder;
    private readonly AiOptions Options;

    public AiConversationService(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiCredentialProtector protector,
        IAiConversationRunner runner,
        IAiChatProviderFactory providerFactory,
        IAiSystemPromptBuilder promptBuilder,
        IAiChangeSetBuilder changeSetBuilder,
        IOptions<AiOptions> options)
    {
        ChangeSetBuilder = changeSetBuilder;
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
        var workspace = await UnitOfWork.Workspaces.GetAsync(workspaceId, true, cancellationToken);
        var isAssistantEnabled = workspace?.AssistantEnabled ?? false;

        if (!isAssistantEnabled)
        {
            yield return AiStreamEvent.Failed("The assistant is turned off for this workspace.");

            yield break;
        }

        var credentials = await UnitOfWork.AiCredentials.GetForUser(userId, cancellationToken);

        if (credentials.Count == 0)
        {
            yield return AiStreamEvent.Failed("No API key is configured for the assistant.");

            yield break;
        }

        var existing = request.ConversationId.HasValue
            ? await UnitOfWork.AiConversations.GetOwned(request.ConversationId.Value, userId, workspaceId, cancellationToken)
            : null;

        var conversationMissing = request.ConversationId.HasValue && existing is null;

        if (conversationMissing)
        {
            yield return AiStreamEvent.Failed("Conversation not found.");

            yield break;
        }

        var provider = ResolveProvider(request.Provider, existing, credentials);
        var credential = credentials.FirstOrDefault(item => item.Provider == provider);

        if (credential is null)
        {
            yield return AiStreamEvent.Failed($"No API key is configured for {provider}.");

            yield break;
        }

        var conversation = existing ?? await CreateConversation(userId, workspaceId, provider, text, cancellationToken);

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

        var assistantMessageId = await PersistTurn(conversation, context, assistantText.ToString(), cancellationToken);
        var changeSetId = await PersistChangeSet(conversation, assistantMessageId, cancellationToken);

        credential.LastUsedAt = DateTime.UtcNow;

        await UnitOfWork.CompleteAsync(cancellationToken);

        if (changeSetId.HasValue)
        {
            yield return AiStreamEvent.ChangeSetProposed(changeSetId.Value);
        }
    }

    private static AiProvider ResolveProvider(
        AiProvider? requested,
        AiConversation? existing,
        IReadOnlyList<UserAiCredential> credentials)
    {
        if (requested.HasValue)
        {
            return requested.Value;
        }

        if (existing is not null)
        {
            return existing.Provider;
        }

        var hasAnthropic = credentials.Any(credential => credential.Provider == AiProvider.Anthropic);

        return hasAnthropic ? AiProvider.Anthropic : credentials[0].Provider;
    }

    private async Task<AiConversation> CreateConversation(
        string userId,
        int workspaceId,
        AiProvider provider,
        string firstMessage,
        CancellationToken cancellationToken)
    {
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
        var history = messages
            .Select(message => AiMessageContent.FromJsonDocument(message.Content).ToChatMessage(message.Role))
            .ToList();

        return TrimHistory(history, Options.MaxHistoryCharacters);
    }

    public static List<AiChatMessage> TrimHistory(List<AiChatMessage> history, int maxCharacters)
    {
        var kept = new List<AiChatMessage>();
        var used = 0;

        for (var index = history.Count - 1; index >= 0; index--)
        {
            var message = history[index];
            var cost = MeasureMessage(message);
            var exceedsBudget = used + cost > maxCharacters && kept.Count > 0;

            if (exceedsBudget)
            {
                break;
            }

            used += cost;
            kept.Add(message);
        }

        kept.Reverse();

        return DropOrphanedToolResults(kept);
    }

    private static List<AiChatMessage> DropOrphanedToolResults(List<AiChatMessage> history)
    {
        var firstUserIndex = history.FindIndex(message => message.Role == AiMessageRole.User);

        if (firstUserIndex <= 0)
        {
            return firstUserIndex == 0 ? history : [];
        }

        return history[firstUserIndex..];
    }

    private static int MeasureMessage(AiChatMessage message)
    {
        var textLength = message.Text?.Length ?? 0;
        var toolCallLength = message.ToolCalls.Sum(call => call.Arguments.RootElement.GetRawText().Length);
        var toolResultLength = message.ToolResults.Sum(result => result.Content.Length);

        return textLength + toolCallLength + toolResultLength;
    }

    private async Task<long> PersistMessage(
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

        return record.Id;
    }

    private async Task<long> PersistTurn(
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

        var assistantMessageId = await PersistMessage(
            conversation,
            assistantMessage,
            AiMessageRole.Assistant,
            lastTurn,
            cancellationToken);

        var hasInvocations = context.Invocations.Count > 0;

        if (!hasInvocations)
        {
            return assistantMessageId;
        }

        var invocations = context.Invocations.Select(invocation => new AiToolInvocation
        {
            ConversationId = conversation.Id,
            MessageId = assistantMessageId,
            ToolName = invocation.ToolName,
            Arguments = invocation.Arguments,
            Result = JsonDocument.Parse(JsonSerializer.Serialize(invocation.Result)),
            ResultTruncated = invocation.Truncated,
            Status = invocation.IsError ? AiToolInvocationStatus.Failed : AiToolInvocationStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
        });

        await UnitOfWork.AiConversations.AddToolInvocations(invocations, cancellationToken);

        return assistantMessageId;
    }

    private async Task<Guid?> PersistChangeSet(
        AiConversation conversation,
        long assistantMessageId,
        CancellationToken cancellationToken)
    {
        var drafts = ChangeSetBuilder.Changes;

        if (drafts.Count == 0)
        {
            return null;
        }

        var changeSet = new AiChangeSet
        {
            Id = Guid.NewGuid(),
            WorkspaceId = conversation.WorkspaceId,
            ConversationId = conversation.Id,
            MessageId = assistantMessageId,
            UserId = conversation.UserId,
            Status = AiChangeSetStatus.Pending,
            CorrelationId = Guid.NewGuid(),
        };

        var changes = drafts.Select((draft, index) => new AiProposedChange
        {
            ChangeSetId = changeSet.Id,
            Sequence = index + 1,
            ToolName = draft.ToolName,
            EntityType = draft.EntityType,
            EntityId = draft.EntityId,
            RefKey = draft.RefKey,
            Summary = draft.Summary,
            Fields = SerializeFields(draft.Fields),
            Payload = draft.Payload,
            ValidationStatus = draft.ValidationStatus,
            ValidationMessage = draft.ValidationMessage,
            ApplyStatus = AiChangeApplyStatus.Pending,
        });

        await UnitOfWork.AiChangeSets.Add(changeSet, changes, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return changeSet.Id;
    }

    private static JsonDocument SerializeFields(List<AiChangeField> fields)
    {
        var json = JsonSerializer.Serialize(fields, FieldSerializerOptions);

        return JsonDocument.Parse(json);
    }
}
