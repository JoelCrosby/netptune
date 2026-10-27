using System.Text.Json;

using Anthropic.Models.Messages;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Ai.Providers;

internal static class AnthropicRequestBuilder
{
    public static MessageCreateParams Create(AiChatRequest request)
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
            CacheControl = new CacheControlEphemeral(),
            OutputConfig = CreateOutputConfig(request.Effort),
        };
    }

    private static OutputConfig? CreateOutputConfig(AiEffort? effort)
    {
        if (!effort.HasValue)
        {
            return null;
        }

        return new OutputConfig { Effort = CreateEffort(effort.Value) };
    }

    private static Effort CreateEffort(AiEffort effort)
    {
        return effort switch
        {
            AiEffort.Low => Effort.Low,
            AiEffort.Medium => Effort.Medium,
            AiEffort.XHigh => Effort.Xhigh,
            AiEffort.Max => Effort.Max,
            _ => Effort.High,
        };
    }

    // The whole schema goes across, so keys beyond properties and required, such as additionalProperties,
    // still reach the model.
    private static ToolUnion CreateTool(AiToolDefinition definition)
    {
        var schema = definition.InputSchema.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);

        return new Tool
        {
            Name = definition.Name,
            Description = definition.Description,
            InputSchema = InputSchema.FromRawUnchecked(schema),
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

            return new UserMessageParam { Content = resultBlocks };
        }

        var isAssistant = message.Role == AiMessageRole.Assistant;

        if (!isAssistant)
        {
            return new UserMessageParam { Content = message.PromptText };
        }

        var replay = AnthropicReplay.Read(message.ProviderPayload);
        var blocks = replay is null ? CreateAssistantBlocks(message) : ReplayAssistantBlocks(message, replay);

        return new AssistantMessageParam { Content = blocks };
    }

    private static List<ContentBlockParam> CreateAssistantBlocks(AiChatMessage message)
    {
        var blocks = new List<ContentBlockParam>();
        var hasText = !string.IsNullOrWhiteSpace(message.Text);

        if (hasText)
        {
            blocks.Add(new TextBlockParam { Text = message.Text! });
        }

        blocks.AddRange(message.ToolCalls.Select(CreateToolUse));

        return blocks;
    }

    // The turn's text arrives as one string, so it goes in the first text slot and later slots are skipped.
    // A call that was dropped for unreadable arguments has no slot to fill.
    private static List<ContentBlockParam> ReplayAssistantBlocks(AiChatMessage message, AnthropicReplay replay)
    {
        var blocks = new List<ContentBlockParam>();
        var callsById = message.ToolCalls.ToDictionary(call => call.Id, StringComparer.Ordinal);
        var replayedCallIds = new HashSet<string>(StringComparer.Ordinal);
        var hasText = !string.IsNullOrWhiteSpace(message.Text);
        var isTextPlaced = false;

        foreach (var block in replay.Blocks)
        {
            switch (block.Type)
            {
                case AnthropicReplayBlockTypes.Thinking:
                    blocks.Add(new ThinkingBlockParam
                    {
                        Thinking = block.Thinking ?? string.Empty,
                        Signature = block.Signature ?? string.Empty,
                    });
                    break;

                case AnthropicReplayBlockTypes.RedactedThinking:
                    blocks.Add(new RedactedThinkingBlockParam { Data = block.Data ?? string.Empty });
                    break;

                case AnthropicReplayBlockTypes.Text when hasText && !isTextPlaced:
                    blocks.Add(new TextBlockParam { Text = message.Text! });
                    isTextPlaced = true;
                    break;

                case AnthropicReplayBlockTypes.ToolUse
                    when callsById.TryGetValue(block.ToolUseId ?? string.Empty, out var call):
                    blocks.Add(CreateToolUse(call));
                    replayedCallIds.Add(call.Id);
                    break;
            }
        }

        if (hasText && !isTextPlaced)
        {
            blocks.Add(new TextBlockParam { Text = message.Text! });
        }

        var unplacedCalls = message.ToolCalls.Where(call => !replayedCallIds.Contains(call.Id));

        blocks.AddRange(unplacedCalls.Select(CreateToolUse));

        return blocks;
    }

    private static ContentBlockParam CreateToolUse(AiToolCall call)
    {
        return new ToolUseBlockParam
        {
            ID = call.Id,
            Name = call.Name,
            Input = CreateInputMap(call.Arguments),
        };
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
}
