using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Ai.Providers;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Ai.Execution;

public sealed class AiConversationService : IAiConversationService
{
    private const int MaximumTitleLength = 80;

    private const string StubbedToolResult =
        "[Result omitted to make room in the context window. Call the tool again if you still need it.]";

    private const int CompactionTargetPercent = 70;
    private const int MaxRecalledQuestions = 5;
    private const int MaxRecalledQuestionLength = 120;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiCredentialProtector Protector;
    private readonly IAiConversationRunner Runner;
    private readonly IAiChatProviderFactory ProviderFactory;
    private readonly IAiSystemPromptBuilder PromptBuilder;
    private readonly IAiChangeSetBuilder ChangeSetBuilder;
    private readonly IAiQuestionSink Questions;
    private readonly IAiTitleGenerator Titles;
    private readonly IAiCancellationRegistry Turns;
    private readonly AiOptions Options;

    public AiConversationService(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiCredentialProtector protector,
        IAiConversationRunner runner,
        IAiChatProviderFactory providerFactory,
        IAiSystemPromptBuilder promptBuilder,
        IAiChangeSetBuilder changeSetBuilder,
        IAiQuestionSink questions,
        IAiTitleGenerator titles,
        IAiCancellationRegistry turns,
        IOptions<AiOptions> options)
    {
        ChangeSetBuilder = changeSetBuilder;
        Questions = questions;
        Titles = titles;
        Turns = turns;
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
        var hasAnswer = request.Answer is not null;

        if (!hasText && !hasAnswer && !request.Retry)
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

        var credentials = await ResolveCredentials(userId, workspaceId, cancellationToken);

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

        var trimmedModel = request.Model?.Trim();
        var requestedModel = string.IsNullOrWhiteSpace(trimmedModel) ? null : trimmedModel;
        var requestedProvider = request.Provider ?? AiModels.ProviderFor(requestedModel);
        var provider = ResolveProvider(requestedProvider, existing, credentials);
        var credential = credentials.FirstOrDefault(item => item.Provider == provider);

        if (credential is null)
        {
            yield return AiStreamEvent.Failed($"No API key is configured for {provider}.");

            yield break;
        }

        var model = ResolveModel(provider, requestedModel, credential.Model);
        var conversation = existing ?? await CreateConversation(
            new AiConversationSeed(userId, workspaceId, provider, model),
            text,
            cancellationToken);

        var switchesModel = existing is not null && AiModels.IsSupported(provider, requestedModel);

        if (switchesModel)
        {
            conversation.Provider = provider;
            conversation.Model = model;
        }

        conversation.RequestedModel = requestedModel;
        conversation.RequestedEffort = request.Effort;

        var effort = ResolveEffort(conversation.Model, request.Effort);

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

        if (request.Retry)
        {
            var rewound = await Rewind(conversation, text, cancellationToken);

            if (rewound is null)
            {
                yield return AiStreamEvent.Failed("There is no message to try again.");

                yield break;
            }

            text = rewound;
        }

        var loaded = await LoadHistory(conversation.Id, cancellationToken);
        var compacted = loaded.Compacted;
        var history = WithRecap(compacted);

        if (compacted.DroppedMessages > 0)
        {
            yield return AiStreamEvent.HistoryCompacted(compacted.DroppedMessages);
        }

        var answer = ResolveAnswer(request.Answer, loaded.PendingQuestion);

        if (answer is not null)
        {
            text = answer.Described;
        }

        var hasMessage = text.Length > 0;

        if (!hasMessage)
        {
            yield return AiStreamEvent.Failed("A message is required.");

            yield break;
        }

        var userMessage = new AiChatMessage
        {
            Role = AiMessageRole.User,
            Text = text,
            Answer = answer?.Answer,
        };

        await PersistMessage(
            conversation,
            new AiMessageDraft { Message = userMessage, Role = AiMessageRole.User },
            cancellationToken);

        var revised = await DescribeRevisedChange(request.Revise, userId, workspaceId, cancellationToken);

        history.Add(WithContext(userMessage, request.Context, revised));

        var apiKey = Protector.Unprotect(credential.Secret);
        var language = AiLanguage.Describe(request.Locale);
        var systemPrompt = await PromptBuilder.Build(request.Locale, cancellationToken);
        var context = new AiRunContext
        {
            Provider = conversation.Provider,
            Model = conversation.Model,
            Effort = effort,
            ApiKey = apiKey,
            SystemPrompt = systemPrompt,
            History = history,
            Permissions = membership.Permissions.ToHashSet(StringComparer.Ordinal),
        };

        var assistantText = new StringBuilder();

        using var turnCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var registration = Turns.Register(conversation.Id, turnCancellation);

        var turn = Runner.Run(context, turnCancellation.Token).GetAsyncEnumerator(turnCancellation.Token);

        bool wasStopped;
        string? failure;

        while (true)
        {
            var step = await ReadNext(turn);
            var streamEvent = step.Event;

            if (streamEvent is null)
            {
                wasStopped = turnCancellation.IsCancellationRequested;
                failure = step.Failure;

                break;
            }

            if (streamEvent.Type == AiStreamEventType.TextDelta && streamEvent.Text is not null)
            {
                assistantText.Append(streamEvent.Text);
            }

            if (streamEvent.Type == AiStreamEventType.ReplyReset)
            {
                assistantText.Clear();
            }

            yield return streamEvent;
        }

        await DisposeTurn(turn);

        if (wasStopped)
        {
            yield return AiStreamEvent.Stopped();
        }

        if (failure is not null)
        {
            yield return AiStreamEvent.Failed(failure);
        }

        var reply = assistantText.ToString();
        var references = AiEntityReferenceReader.Read(context.Invocations.Select(invocation => new AiToolResultText
        {
            ToolName = invocation.ToolName,
            Content = invocation.Result,
        }));

        if (references.Count > 0)
        {
            yield return AiStreamEvent.EntitiesReferenced(references);
        }

        var persisted = await UnitOfWork.Transaction(async () =>
        {
            var assistantMessageId = await PersistTurn(conversation, context, reply, cancellationToken);
            var pendingChangeSetId = await PersistChangeSet(conversation, assistantMessageId, cancellationToken);

            await MarkCredentialUsed(credential, cancellationToken);

            await UnitOfWork.CompleteAsync(cancellationToken);

            return new PersistedTurn(assistantMessageId, pendingChangeSetId);
        });

        if (persisted.ChangeSetId.HasValue)
        {
            yield return AiStreamEvent.ChangeSetProposed(persisted.ChangeSetId.Value);
        }

        var titleRequest = new AiTitleRequest
        {
            Provider = provider,
            ApiKey = apiKey,
            UserMessage = text,
            AssistantMessage = reply,
            Language = language,
        };

        await ApplyTitle(conversation, persisted.MessageId, titleRequest, existing is null, cancellationToken);

        var usage = await UnitOfWork.AiConversations.GetUsage(conversation.Id, cancellationToken);

        yield return AiStreamEvent.UsageUpdated(usage.WithCost(conversation.Model));
    }

    private sealed record TurnStep(AiStreamEvent? Event, string? Failure);

    private static async Task<TurnStep> ReadNext(IAsyncEnumerator<AiStreamEvent> turn)
    {
        try
        {
            var moved = await turn.MoveNextAsync();

            return new TurnStep(moved ? turn.Current : null, null);
        }
        catch (OperationCanceledException)
        {
            return new TurnStep(null, null);
        }
        catch (Exception exception)
        {
            var described = AiProviderErrors.Describe(exception);

            return new TurnStep(null, described ?? "The assistant could not reach the provider.");
        }
    }

    private static async Task DisposeTurn(IAsyncEnumerator<AiStreamEvent> turn)
    {
        try
        {
            await turn.DisposeAsync();
        }
        catch (OperationCanceledException)
        {
            /* The turn was stopped, so its provider stream ends the same way. */
        }
    }

    private sealed record PersistedTurn(long MessageId, Guid? ChangeSetId);

    private async Task ApplyTitle(
        AiConversation conversation,
        long assistantMessageId,
        AiTitleRequest request,
        bool isNewConversation,
        CancellationToken cancellationToken)
    {
        var title = await TryCreateTitle(request, isNewConversation, cancellationToken);

        if (title.Title is null)
        {
            return;
        }

        conversation.Title = title.Title;

        await UnitOfWork.CompleteAsync(cancellationToken);
        await UnitOfWork.AiConversations.AddMessageUsage(assistantMessageId, title.Usage, cancellationToken);
    }

    private static AiChatMessage WithContext(
        AiChatMessage message,
        AiClientContext? clientContext,
        string? revised)
    {
        var viewing = DescribeClientContext(clientContext);
        var text = new StringBuilder(message.Text);

        if (viewing is not null)
        {
            text.Append($"\n\n<viewing>\n{viewing}\n</viewing>");
        }

        if (revised is not null)
        {
            text.Append($"\n\n<revising>\n{revised}\n</revising>");
        }

        var hasContext = viewing is not null || revised is not null;

        if (!hasContext)
        {
            return message;
        }

        return message with { Text = text.ToString() };
    }

    // The reviewer picked one proposal off the review surface. Restating what it does, rather than
    // its id, keeps the assistant working from the same reading the reviewer had.
    private async Task<string?> DescribeRevisedChange(
        AiReviseRequest? revise,
        string userId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (revise is null)
        {
            return null;
        }

        var changeSet = await UnitOfWork.AiChangeSets.GetOwned(
            revise.ChangeSetId,
            userId,
            workspaceId,
            cancellationToken);

        if (changeSet is null)
        {
            return null;
        }

        var changes = await UnitOfWork.AiChangeSets.GetChanges(changeSet.Id, cancellationToken);
        var change = changes.FirstOrDefault(candidate => candidate.Id == revise.ChangeId);

        if (change is null)
        {
            return null;
        }

        var lines = new List<string>
        {
            "The user is asking you to rework this proposal, which you made earlier in this conversation.",
            $"tool: {change.ToolName}",
            $"summary: {change.Summary}",
        };

        AddContextLine(lines, "entity", Describe(change.EntityType, change.EntityId));

        foreach (var field in AiChangeFieldSerializer.Deserialize(change.Fields))
        {
            AddContextLine(lines, $"field {field.Name}", DescribeFieldChange(field));
        }

        lines.Add("Propose a replacement with the corrections the user asks for. Do not repeat the proposal unchanged.");

        return string.Join("\n", lines);
    }

    private static string DescribeFieldChange(AiChangeFieldViewModel field)
    {
        var before = string.IsNullOrWhiteSpace(field.Before) ? "(none)" : field.Before;
        var after = string.IsNullOrWhiteSpace(field.After) ? "(none)" : field.After;

        return $"{before} -> {after}";
    }

    private static string? DescribeClientContext(AiClientContext? clientContext)
    {
        if (clientContext is null)
        {
            return null;
        }

        var lines = new List<string>();

        AddContextLine(lines, "view", clientContext.View);
        AddContextLine(lines, "project", Describe(clientContext.ProjectName, clientContext.ProjectId));
        AddContextLine(lines, "board", Describe(clientContext.BoardName, clientContext.BoardId));
        AddContextLine(lines, "sprint", Describe(clientContext.SprintName, clientContext.SprintId));
        AddContextLine(lines, "task", Describe(clientContext.TaskName, clientContext.TaskSystemId));

        if (lines.Count == 0)
        {
            return null;
        }

        return string.Join("\n", lines);
    }

    private static string? Describe(string? name, object? identifier)
    {
        var hasName = !string.IsNullOrWhiteSpace(name);
        var hasIdentifier = identifier is not null;

        if (hasName && hasIdentifier)
        {
            return $"{name} ({identifier})";
        }

        return hasName ? name : identifier?.ToString();
    }

    private static void AddContextLine(List<string> lines, string name, string? value)
    {
        var hasValue = !string.IsNullOrWhiteSpace(value);

        if (!hasValue)
        {
            return;
        }

        lines.Add($"{name}: {value}");
    }

    private async Task<List<AiResolvedCredential>> ResolveCredentials(
        string userId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var userCredentials = await UnitOfWork.AiCredentials.GetForUser(userId, cancellationToken);
        var workspaceCredentials = await UnitOfWork.WorkspaceAiCredentials.GetForWorkspace(
            workspaceId,
            cancellationToken);

        return AiCredentialResolution.Resolve(userCredentials, workspaceCredentials);
    }

    private async Task MarkCredentialUsed(AiResolvedCredential credential, CancellationToken cancellationToken)
    {
        var isWorkspaceKey = credential.Source == AiCredentialSource.Workspace;

        if (isWorkspaceKey)
        {
            var workspaceCredential = await UnitOfWork.WorkspaceAiCredentials.GetAsync(credential.Id, cancellationToken: cancellationToken);

            if (workspaceCredential is not null)
            {
                workspaceCredential.LastUsedAt = DateTime.UtcNow;
            }

            return;
        }

        var userCredential = await UnitOfWork.AiCredentials.GetAsync(credential.Id, cancellationToken: cancellationToken);

        if (userCredential is not null)
        {
            userCredential.LastUsedAt = DateTime.UtcNow;
        }
    }

    private static AiProvider ResolveProvider(
        AiProvider? requested,
        AiConversation? existing,
        IReadOnlyList<AiResolvedCredential> credentials)
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

    private AiEffort? ResolveEffort(string model, AiEffort? requested)
    {
        var supportsEffort = AiModels.SupportsEffort(model);

        if (!supportsEffort)
        {
            return null;
        }

        return requested ?? Options.DefaultEffort;
    }

    private async Task<AiTitleResult> TryCreateTitle(
        AiTitleRequest request,
        bool isNewConversation,
        CancellationToken cancellationToken)
    {
        var shouldGenerate = isNewConversation && Options.GenerateTitles;

        if (!shouldGenerate)
        {
            return new AiTitleResult();
        }

        try
        {
            return await Titles.Generate(request, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new AiTitleResult();
        }
    }

    private string ResolveModel(AiProvider provider, string? requestedModel, string? credentialModel)
    {
        var isRequestedSupported = AiModels.IsSupported(provider, requestedModel);

        if (isRequestedSupported)
        {
            return requestedModel!;
        }

        var isCredentialSupported = AiModels.IsSupported(provider, credentialModel);

        if (isCredentialSupported)
        {
            return credentialModel!;
        }

        return ProviderFactory.Resolve(provider).DefaultModel;
    }

    private sealed record AiConversationSeed(
        string UserId,
        int WorkspaceId,
        AiProvider Provider,
        string Model);

    private async Task<AiConversation> CreateConversation(
        AiConversationSeed seed,
        string firstMessage,
        CancellationToken cancellationToken)
    {
        var conversation = new AiConversation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = seed.WorkspaceId,
            UserId = seed.UserId,
            Title = CreateTitle(firstMessage),
            Provider = seed.Provider,
            Model = seed.Model,
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

    private sealed record LoadedHistory(AiCompactedHistory Compacted, AiQuestion? PendingQuestion);

    private async Task<LoadedHistory> LoadHistory(Guid conversationId, CancellationToken cancellationToken)
    {
        var messages = await UnitOfWork.AiConversations.GetMessages(conversationId, cancellationToken);
        var contents = messages
            .Select(message => AiMessageContent.FromJsonDocument(message.Content))
            .ToList();

        var replayed = contents
            .Select((content, index) => content.ToChatMessage(messages[index].Role))
            .ToList();

        var history = DropUnansweredToolCalls(replayed);
        var compacted = Compact(history, Options.MaxHistoryCharacters);
        var pending = FindPendingQuestion(messages, contents);

        return new LoadedHistory(compacted, pending);
    }

    // A question is only open while it is the last thing in the conversation. Anything said after it
    // moved the conversation on, whether or not it was an answer.
    private static AiQuestion? FindPendingQuestion(List<AiMessage> messages, List<AiMessageContent> contents)
    {
        var isEmpty = messages.Count == 0;

        if (isEmpty)
        {
            return null;
        }

        var isAssistantLast = messages[^1].Role == AiMessageRole.Assistant;

        if (!isAssistantLast)
        {
            return null;
        }

        return contents[^1].Question;
    }

    private sealed record ResolvedAnswer(AiQuestionAnswer Answer, string Described);

    private static ResolvedAnswer? ResolveAnswer(AiAnswerRequest? requested, AiQuestion? question)
    {
        if (requested is null || question is null)
        {
            return null;
        }

        var isForPendingQuestion = requested.QuestionId == question.Id;

        if (!isForPendingQuestion)
        {
            return null;
        }

        var typed = requested.Text?.Trim();
        var hasTyped = !string.IsNullOrWhiteSpace(typed);

        List<string> chosen = hasTyped ? [] : MatchOptions(requested.SelectedLabels, question);

        var hasChoice = hasTyped || chosen.Count > 0;

        if (!hasChoice)
        {
            return null;
        }

        var answer = new AiQuestionAnswer
        {
            QuestionId = question.Id,
            SelectedLabels = chosen,
            Text = hasTyped ? typed : null,
        };

        var described = answer.Describe(question);

        return new ResolvedAnswer(answer, described);
    }

    // Only labels the assistant offered, so a card left open in another tab cannot answer with an option
    // the question never had.
    private static List<string> MatchOptions(List<string> selected, AiQuestion question)
    {
        return question.Options
            .Select(option => option.Label)
            .Where(label => selected.Contains(label, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    // A tool call replayed with nothing answering it is a tool_use block with no tool_result, which the
    // provider rejects outright. Turns stored before the results were left out carry exactly that.
    public static List<AiChatMessage> DropUnansweredToolCalls(List<AiChatMessage> history)
    {
        var replayable = new List<AiChatMessage>(history.Count);

        for (var index = 0; index < history.Count; index++)
        {
            var message = history[index];
            var hasToolCalls = message.ToolCalls.Count > 0;

            if (!hasToolCalls)
            {
                replayable.Add(message);

                continue;
            }

            var isAnswered = IsAnswered(history, index);

            replayable.Add(isAnswered ? message : message with { ToolCalls = [] });
        }

        return replayable;
    }

    private static bool IsAnswered(List<AiChatMessage> history, int index)
    {
        var hasFollowingMessage = index + 1 < history.Count;

        if (!hasFollowingMessage)
        {
            return false;
        }

        var answeredIds = history[index + 1].ToolResults
            .Select(result => result.ToolCallId)
            .ToHashSet(StringComparer.Ordinal);

        return history[index].ToolCalls.All(call => answeredIds.Contains(call.Id));
    }

    public static List<AiChatMessage> TrimHistory(List<AiChatMessage> history, int maxCharacters)
    {
        return Compact(history, maxCharacters).Messages;
    }

    public static AiCompactedHistory Compact(List<AiChatMessage> history, int maxCharacters)
    {
        var compacted = CompactToolResults(history, maxCharacters);
        var kept = new List<AiChatMessage>();
        var used = 0;

        for (var index = compacted.Count - 1; index >= 0; index--)
        {
            var message = compacted[index];
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

        var messages = DropOrphanedToolResults(kept);
        var dropped = compacted.Take(compacted.Count - messages.Count).ToList();

        return new AiCompactedHistory
        {
            Messages = messages,
            DroppedMessages = dropped.Count,
            DroppedQuestions = ReadQuestions(dropped),
        };
    }

    private static List<string> ReadQuestions(List<AiChatMessage> dropped)
    {
        return dropped
            .Where(message => message.Role == AiMessageRole.User)
            .Select(message => message.Text ?? string.Empty)
            .Where(text => text.Length > 0)
            .Select(Shorten)
            .TakeLast(MaxRecalledQuestions)
            .ToList();
    }

    private static string Shorten(string text)
    {
        var collapsed = text.ReplaceLineEndings(" ").Trim();

        return collapsed.Length <= MaxRecalledQuestionLength
            ? collapsed
            : $"{collapsed[..MaxRecalledQuestionLength].TrimEnd()}…";
    }

    private static List<AiChatMessage> WithRecap(AiCompactedHistory compacted)
    {
        var messages = compacted.Messages;
        var hasRecap = compacted.DroppedMessages > 0 && messages.Count > 0;

        if (!hasRecap)
        {
            return messages;
        }

        var questions = compacted.DroppedQuestions.Count == 0
            ? string.Empty
            : $" They asked: {string.Join("; ", compacted.DroppedQuestions.Select(question => $"“{question}”"))}.";

        var recap = $"[Earlier context] {compacted.DroppedMessages} earlier messages in this conversation "
            + "are no longer available, to stay within the context window."
            + questions
            + " Look anything up again with the tools rather than guessing at it.";

        var first = messages[0];
        var rewritten = first with { Text = $"{recap}\n\n{first.Text}" };

        return [rewritten, .. messages.Skip(1)];
    }

    private static List<AiChatMessage> CompactToolResults(List<AiChatMessage> history, int maxCharacters)
    {
        var total = history.Sum(MeasureMessage);
        var isWithinBudget = total <= maxCharacters;

        if (isWithinBudget)
        {
            return history;
        }

        var currentTurn = history.FindLastIndex(message => message.Role == AiMessageRole.User);
        var compacted = new List<AiChatMessage>(history);

        var target = maxCharacters * CompactionTargetPercent / 100;

        for (var index = 0; index < currentTurn; index++)
        {
            var isOverTarget = total > target;

            if (!isOverTarget)
            {
                break;
            }

            var message = compacted[index];
            var hasResults = message.ToolResults.Count > 0;

            if (!hasResults)
            {
                continue;
            }

            var stubbed = message with { ToolResults = message.ToolResults.Select(Stub).ToList() };

            total -= MeasureMessage(message) - MeasureMessage(stubbed);
            compacted[index] = stubbed;
        }

        return compacted;
    }

    private static AiToolResult Stub(AiToolResult result)
    {
        return result with { Content = StubbedToolResult };
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

    private sealed record AiMessageDraft
    {
        public required AiChatMessage Message { get; init; }

        public AiMessageRole Role { get; init; }

        public AiChatTurn? Turn { get; init; }

        public List<string> ToolsRun { get; init; } = [];

        public AiUsage ExtraUsage { get; init; } = new();
    }

    private async Task<long> PersistMessage(
        AiConversation conversation,
        AiMessageDraft draft,
        CancellationToken cancellationToken)
    {
        var sequence = await UnitOfWork.AiConversations.GetNextSequence(conversation.Id, cancellationToken);
        var content = AiMessageContent.FromChatMessage(draft.Message) with { ToolsRun = draft.ToolsRun };
        var turn = draft.Turn;
        var usage = turn?.Usage ?? new AiUsage();
        var extra = draft.ExtraUsage;
        var record = new AiMessage
        {
            ConversationId = conversation.Id,
            Sequence = sequence,
            Role = draft.Role,
            Content = content.ToJsonDocument(),
            ProviderPayload = turn?.ProviderPayload,
            Provider = conversation.Provider,
            Model = conversation.Model,
            Status = AiMessageStatus.Complete,
            FinishReason = turn?.FinishReason,
            InputTokens = usage.InputTokens + extra.InputTokens,
            OutputTokens = usage.OutputTokens + extra.OutputTokens,
            CacheReadTokens = usage.CacheReadTokens + extra.CacheReadTokens,
            CacheCreationTokens = usage.CacheCreationTokens + extra.CacheCreationTokens,
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

        // Tool calls are left out on purpose: nothing persists the results that would answer them.
        var assistantMessage = new AiChatMessage
        {
            Role = AiMessageRole.Assistant,
            Text = assistantText,
            Question = Questions.Pending,
        };

        var toolsRun = context.Invocations.Select(invocation => invocation.ToolName).ToList();
        var assistantMessageId = await PersistMessage(
            conversation,
            new AiMessageDraft
            {
                Message = assistantMessage,
                Role = AiMessageRole.Assistant,
                Turn = lastTurn,
                ToolsRun = toolsRun,
            },
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
            Fields = AiChangeFieldSerializer.Serialize(draft.Fields),
            Payload = draft.Payload,
            ValidationStatus = draft.ValidationStatus,
            ValidationMessage = draft.ValidationMessage,
            ApplyStatus = AiChangeApplyStatus.Pending,
        });

        await UnitOfWork.AiChangeSets.Add(changeSet, changes, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return changeSet.Id;
    }

    private async Task<string?> Rewind(AiConversation conversation, string replacement, CancellationToken cancellationToken)
    {
        var messages = await UnitOfWork.AiConversations.GetMessages(conversation.Id, cancellationToken);
        var lastUserMessage = messages.LastOrDefault(message => message.Role == AiMessageRole.User);

        if (lastUserMessage is null)
        {
            return null;
        }

        var original = AiMessageContent.FromJsonDocument(lastUserMessage.Content).ToChatMessage(AiMessageRole.User).Text ?? string.Empty;
        var hasReplacement = replacement.Length > 0;
        var text = hasReplacement ? replacement : original;

        if (text.Length == 0)
        {
            return null;
        }

        await DiscardPendingChangeSet(conversation, cancellationToken);

        var removed = await UnitOfWork.AiConversations.RemoveMessagesFrom(
            conversation.Id,
            lastUserMessage.Sequence,
            cancellationToken);

        conversation.MessageCount = Math.Max(0, conversation.MessageCount - removed);

        return text;
    }

    private async Task DiscardPendingChangeSet(AiConversation conversation, CancellationToken cancellationToken)
    {
        var pending = await UnitOfWork.AiChangeSets.GetPending(
            conversation.Id,
            conversation.UserId,
            conversation.WorkspaceId,
            cancellationToken);

        if (pending is null)
        {
            return;
        }

        pending.Status = AiChangeSetStatus.Discarded;
    }
}
