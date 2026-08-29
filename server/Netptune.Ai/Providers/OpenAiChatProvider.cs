using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;

using OpenAI.Chat;

using AiProviderKind = Netptune.Core.Enums.AiProvider;

namespace Netptune.Ai.Providers;

public sealed class OpenAiChatProvider : IAiChatProvider
{
    private readonly AiOptions Options;

    public OpenAiChatProvider(IOptions<AiOptions> options)
    {
        Options = options.Value;
    }

    public AiProviderKind Provider => AiProviderKind.OpenAi;

    public string DefaultModel => Options.OpenAiModel;

    public async IAsyncEnumerable<AiProviderStreamEvent> Stream(
        AiChatRequest request,
        string apiKey,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var client = new ChatClient(request.Model, new ApiKeyCredential(apiKey));
        var messages = CreateMessages(request);
        var options = CreateOptions(request);
        var text = new StringBuilder();
        var pendingCalls = new Dictionary<int, PendingToolCall>();
        var usage = new AiUsage();
        string? finishReason = null;

        var updates = client.CompleteChatStreamingAsync(messages, options, cancellationToken);

        await foreach (var update in updates)
        {
            foreach (var part in update.ContentUpdate)
            {
                var hasText = !string.IsNullOrEmpty(part.Text);

                if (!hasText)
                {
                    continue;
                }

                text.Append(part.Text);

                yield return AiProviderStreamEvent.Delta(part.Text);
            }

            foreach (var toolUpdate in update.ToolCallUpdates)
            {
                TrackToolUpdate(toolUpdate, pendingCalls);
            }

            if (update.FinishReason.HasValue)
            {
                finishReason = update.FinishReason.Value.ToString();
            }

            if (update.Usage is not null)
            {
                usage = new AiUsage
                {
                    InputTokens = update.Usage.InputTokenCount,
                    OutputTokens = update.Usage.OutputTokenCount,
                };
            }
        }

        var toolCalls = pendingCalls.Values
            .OrderBy(call => call.Index)
            .Select(CreateToolCall)
            .ToList();

        yield return AiProviderStreamEvent.Completed(new AiChatTurn
        {
            Text = text.ToString(),
            ToolCalls = toolCalls,
            Usage = usage,
            FinishReason = finishReason,
        });
    }

    private static void TrackToolUpdate(
        StreamingChatToolCallUpdate toolUpdate,
        Dictionary<int, PendingToolCall> pendingCalls)
    {
        var hasCall = pendingCalls.TryGetValue(toolUpdate.Index, out var call);

        if (!hasCall)
        {
            call = new PendingToolCall
            {
                Index = toolUpdate.Index,
                Arguments = new StringBuilder(),
            };

            pendingCalls[toolUpdate.Index] = call;
        }

        var hasId = !string.IsNullOrEmpty(toolUpdate.ToolCallId);

        if (hasId)
        {
            call!.Id = toolUpdate.ToolCallId;
        }

        var hasName = !string.IsNullOrEmpty(toolUpdate.FunctionName);

        if (hasName)
        {
            call!.Name = toolUpdate.FunctionName;
        }

        var argumentsUpdate = toolUpdate.FunctionArgumentsUpdate?.ToString();
        var hasArguments = !string.IsNullOrEmpty(argumentsUpdate);

        if (hasArguments)
        {
            call!.Arguments.Append(argumentsUpdate);
        }
    }

    private static AiToolCall CreateToolCall(PendingToolCall call)
    {
        var raw = call.Arguments.ToString();
        var json = string.IsNullOrWhiteSpace(raw) ? "{}" : raw;

        return new AiToolCall
        {
            Id = call.Id,
            Name = call.Name,
            Arguments = JsonDocument.Parse(json),
        };
    }

    private ChatCompletionOptions CreateOptions(AiChatRequest request)
    {
        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = request.MaxOutputTokens,
        };

        if (request.Effort.HasValue)
        {
#pragma warning disable OPENAI001
            options.ReasoningEffortLevel = CreateEffort(request.Effort.Value);
#pragma warning restore OPENAI001
        }

        foreach (var tool in request.Tools)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(
                tool.Name,
                tool.Description,
                BinaryData.FromString(tool.InputSchema.RootElement.GetRawText())));
        }

        return options;
    }

#pragma warning disable OPENAI001
    // The OpenAI levels stop at high, so the two Anthropic levels above it collapse onto it.
    private static ChatReasoningEffortLevel CreateEffort(AiEffort effort)
    {
        return effort switch
        {
            AiEffort.Low => ChatReasoningEffortLevel.Low,
            AiEffort.Medium => ChatReasoningEffortLevel.Medium,
            _ => ChatReasoningEffortLevel.High,
        };
    }
#pragma warning restore OPENAI001

    private static List<ChatMessage> CreateMessages(AiChatRequest request)
    {
        var messages = new List<ChatMessage> { new SystemChatMessage(request.SystemPrompt) };

        foreach (var message in request.Messages)
        {
            messages.AddRange(CreateMessage(message));
        }

        return messages;
    }

    private static IEnumerable<ChatMessage> CreateMessage(AiChatMessage message)
    {
        var isToolResult = message.ToolResults.Count > 0;

        if (isToolResult)
        {
            return message.ToolResults.Select(result =>
                (ChatMessage)new ToolChatMessage(result.ToolCallId, result.Content));
        }

        var isAssistant = message.Role == AiMessageRole.Assistant;

        if (!isAssistant)
        {
            return [new UserChatMessage(message.Text ?? string.Empty)];
        }

        var hasToolCalls = message.ToolCalls.Count > 0;

        if (!hasToolCalls)
        {
            return [new AssistantChatMessage(message.Text ?? string.Empty)];
        }

        var calls = message.ToolCalls.Select(call => ChatToolCall.CreateFunctionToolCall(
            call.Id,
            call.Name,
            BinaryData.FromString(call.Arguments.RootElement.GetRawText())));

        return [new AssistantChatMessage(calls)];
    }

    private sealed record PendingToolCall
    {
        public int Index { get; init; }

        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public required StringBuilder Arguments { get; init; }
    }
}
