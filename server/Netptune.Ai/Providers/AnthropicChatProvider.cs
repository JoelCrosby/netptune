using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Anthropic;
using Anthropic.Models.Messages;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;

using AiProviderKind = Netptune.Core.Enums.AiProvider;

namespace Netptune.Ai.Providers;

public sealed class AnthropicChatProvider : IAiChatProvider
{
    private readonly AiOptions Options;

    public AnthropicChatProvider(IOptions<AiOptions> options)
    {
        Options = options.Value;
    }

    public AiProviderKind Provider => AiProviderKind.Anthropic;

    public string DefaultModel => Options.AnthropicModel;

    public async IAsyncEnumerable<AiProviderStreamEvent> Stream(
        AiChatRequest request,
        string apiKey,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var client = new AnthropicClient { ApiKey = apiKey };
        var parameters = CreateParameters(request);
        var text = new StringBuilder();
        var pendingBlocks = new Dictionary<long, PendingToolBlock>();
        var toolCalls = new List<AiToolCall>();
        var usage = new AiUsage();
        string? finishReason = null;

        await foreach (var streamEvent in client.Messages.CreateStreaming(parameters, cancellationToken))
        {
            if (streamEvent.TryPickContentBlockStart(out var blockStart))
            {
                TrackBlockStart(blockStart, pendingBlocks);

                continue;
            }

            if (streamEvent.TryPickContentBlockDelta(out var blockDelta))
            {
                var delta = blockDelta.Delta;

                if (delta.TryPickText(out var textDelta))
                {
                    text.Append(textDelta.Text);

                    yield return AiProviderStreamEvent.Delta(textDelta.Text);

                    continue;
                }

                if (delta.TryPickInputJson(out var jsonDelta))
                {
                    AppendToolArguments(blockDelta.Index, jsonDelta.PartialJson, pendingBlocks);
                }

                continue;
            }

            if (streamEvent.TryPickDelta(out var messageDelta))
            {
                finishReason = messageDelta.Delta.StopReason?.ToString();
                usage = CreateUsage(messageDelta.Usage);
            }
        }

        toolCalls.AddRange(pendingBlocks.Values.OrderBy(block => block.Index).Select(CreateToolCall));

        yield return AiProviderStreamEvent.Completed(new AiChatTurn
        {
            Text = text.ToString(),
            ToolCalls = toolCalls,
            Usage = usage,
            FinishReason = finishReason,
        });
    }

    private static void TrackBlockStart(
        RawContentBlockStartEvent blockStart,
        Dictionary<long, PendingToolBlock> pendingBlocks)
    {
        var isToolUse = blockStart.ContentBlock.TryPickToolUse(out var toolUse);

        if (!isToolUse)
        {
            return;
        }

        pendingBlocks[blockStart.Index] = new PendingToolBlock
        {
            Index = blockStart.Index,
            Id = toolUse!.ID,
            Name = toolUse.Name,
            Arguments = new StringBuilder(),
        };
    }

    private static void AppendToolArguments(
        long index,
        string partialJson,
        Dictionary<long, PendingToolBlock> pendingBlocks)
    {
        var hasBlock = pendingBlocks.TryGetValue(index, out var block);

        if (!hasBlock)
        {
            return;
        }

        block!.Arguments.Append(partialJson);
    }

    private static AiToolCall CreateToolCall(PendingToolBlock block)
    {
        var raw = block.Arguments.ToString();
        var json = string.IsNullOrWhiteSpace(raw) ? "{}" : raw;

        return new AiToolCall
        {
            Id = block.Id,
            Name = block.Name,
            Arguments = JsonDocument.Parse(json),
        };
    }

    private static AiUsage CreateUsage(MessageDeltaUsage usage)
    {
        return new AiUsage
        {
            InputTokens = (int)(usage.InputTokens ?? 0),
            OutputTokens = (int)usage.OutputTokens,
            CacheReadTokens = (int)(usage.CacheReadInputTokens ?? 0),
        };
    }

    private static MessageCreateParams CreateParameters(AiChatRequest request)
    {
        var system = new List<TextBlockParam>
        {
            new()
            {
                Text = request.SystemPrompt,
                CacheControl = new CacheControlEphemeral(),
            },
        };

        return new MessageCreateParams
        {
            Model = request.Model,
            MaxTokens = request.MaxOutputTokens,
            System = system,
            Tools = request.Tools.Select(CreateTool).ToList(),
            Messages = request.Messages.Select(CreateMessage).ToList(),
        };
    }

    private static ToolUnion CreateTool(AiToolDefinition definition)
    {
        var schema = definition.InputSchema.RootElement;
        var properties = new Dictionary<string, JsonElement>();
        var hasProperties = schema.TryGetProperty("properties", out var propertiesElement);

        if (hasProperties)
        {
            foreach (var property in propertiesElement.EnumerateObject())
            {
                properties[property.Name] = property.Value;
            }
        }

        var required = new List<string>();
        var hasRequired = schema.TryGetProperty("required", out var requiredElement);

        if (hasRequired)
        {
            required.AddRange(requiredElement.EnumerateArray().Select(item => item.GetString()!));
        }

        return new Tool
        {
            Name = definition.Name,
            Description = definition.Description,
            InputSchema = new()
            {
                Properties = properties,
                Required = required,
            },
        };
    }

    private static MessageParam CreateMessage(AiChatMessage message)
    {
        var isToolResult = message.ToolResults.Count > 0;

        if (isToolResult)
        {
            var resultBlocks = message.ToolResults
                .Select(result => (ContentBlockParam)new ToolResultBlockParam
                {
                    ToolUseID = result.ToolCallId,
                    Content = result.Content,
                    IsError = result.IsError,
                })
                .ToList();

            return new MessageParam { Role = Role.User, Content = resultBlocks };
        }

        var isAssistant = message.Role == AiMessageRole.Assistant;

        if (!isAssistant)
        {
            return new MessageParam { Role = Role.User, Content = message.Text ?? string.Empty };
        }

        var blocks = new List<ContentBlockParam>();
        var hasText = !string.IsNullOrWhiteSpace(message.Text);

        if (hasText)
        {
            blocks.Add(new TextBlockParam { Text = message.Text! });
        }

        foreach (var call in message.ToolCalls)
        {
            blocks.Add(new ToolUseBlockParam
            {
                ID = call.Id,
                Name = call.Name,
                Input = CreateInputMap(call.Arguments),
            });
        }

        return new MessageParam { Role = Role.Assistant, Content = blocks };
    }

    private static Dictionary<string, JsonElement> CreateInputMap(JsonDocument arguments)
    {
        var map = new Dictionary<string, JsonElement>();
        var isObject = arguments.RootElement.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return map;
        }

        foreach (var property in arguments.RootElement.EnumerateObject())
        {
            map[property.Name] = property.Value.Clone();
        }

        return map;
    }

    private sealed record PendingToolBlock
    {
        public long Index { get; init; }

        public required string Id { get; init; }

        public required string Name { get; init; }

        public required StringBuilder Arguments { get; init; }
    }
}
