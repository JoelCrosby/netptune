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
        using var client = new AnthropicClient { ApiKey = apiKey };
        var parameters = AnthropicRequestBuilder.Create(request);
        var text = new StringBuilder();
        var pendingBlocks = new Dictionary<long, PendingToolBlock>();
        var replayBlocks = new SortedDictionary<long, ReplayBlock>();
        var toolCalls = new List<AiToolCall>();
        var usage = new AiUsage();
        string? finishReason = null;

        await foreach (var streamEvent in client.Messages.CreateStreaming(parameters, cancellationToken))
        {
            if (streamEvent.TryPickContentBlockStart(out var blockStart))
            {
                TrackBlockStart(blockStart, pendingBlocks);
                TrackReplayBlock(blockStart, replayBlocks);

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

                    continue;
                }

                AppendThinking(blockDelta, replayBlocks);

                continue;
            }

            if (streamEvent.TryPickDelta(out var messageDelta))
            {
                finishReason = messageDelta.Delta.StopReason?.ToString();
                usage = CreateUsage(messageDelta.Usage);
            }
        }

        var completedCalls = pendingBlocks.Values
            .OrderBy(block => block.Index)
            .Select(CreateToolCall)
            .OfType<AiToolCall>();

        toolCalls.AddRange(completedCalls);

        yield return AiProviderStreamEvent.Completed(new AiChatTurn
        {
            Text = text.ToString(),
            ToolCalls = toolCalls,
            ProviderPayload = CreateReplayPayload(replayBlocks),
            Usage = usage,
            FinishReason = finishReason,
        });
    }

    // The models think by default, and inside a tool loop the API expects each assistant turn back with
    // its thinking blocks unchanged and in place. The block order is kept so the replay can restore it.
    private static void TrackReplayBlock(
        RawContentBlockStartEvent blockStart,
        SortedDictionary<long, ReplayBlock> replayBlocks)
    {
        var block = blockStart.ContentBlock;

        if (block.TryPickThinking(out var thinking))
        {
            replayBlocks[blockStart.Index] = new ReplayBlock
            {
                Type = AnthropicReplayBlockTypes.Thinking,
                Thinking = new StringBuilder(thinking.Thinking),
                Signature = new StringBuilder(thinking.Signature),
            };

            return;
        }

        if (block.TryPickRedactedThinking(out var redacted))
        {
            replayBlocks[blockStart.Index] = new ReplayBlock
            {
                Type = AnthropicReplayBlockTypes.RedactedThinking,
                Data = redacted.Data,
            };

            return;
        }

        if (block.TryPickText(out _))
        {
            replayBlocks[blockStart.Index] = new ReplayBlock { Type = AnthropicReplayBlockTypes.Text };

            return;
        }

        if (block.TryPickToolUse(out var toolUse))
        {
            replayBlocks[blockStart.Index] = new ReplayBlock
            {
                Type = AnthropicReplayBlockTypes.ToolUse,
                ToolUseId = toolUse.ID,
            };
        }
    }

    private static void AppendThinking(
        RawContentBlockDeltaEvent blockDelta,
        SortedDictionary<long, ReplayBlock> replayBlocks)
    {
        var hasBlock = replayBlocks.TryGetValue(blockDelta.Index, out var block);

        if (!hasBlock)
        {
            return;
        }

        if (blockDelta.Delta.TryPickThinking(out var thinkingDelta))
        {
            block!.Thinking?.Append(thinkingDelta.Thinking);

            return;
        }

        if (blockDelta.Delta.TryPickSignature(out var signatureDelta))
        {
            block!.Signature?.Append(signatureDelta.Signature);
        }
    }

    private static JsonDocument? CreateReplayPayload(SortedDictionary<long, ReplayBlock> replayBlocks)
    {
        var hasThinking = replayBlocks.Values.Any(block => block.Thinking is not null || block.Data is not null);

        if (!hasThinking)
        {
            return null;
        }

        var blocks = replayBlocks.Values.Select(block => new AnthropicReplayBlock
        {
            Type = block.Type,
            Thinking = block.Thinking?.ToString(),
            Signature = block.Signature?.ToString(),
            Data = block.Data,
            ToolUseId = block.ToolUseId,
        });

        return new AnthropicReplay { Blocks = [.. blocks] }.ToDocument();
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

    // A reply cut off by the output limit leaves its last call's arguments unfinished. That call is
    // dropped, and the finish reason tells the runner why the reply stopped.
    private static AiToolCall? CreateToolCall(PendingToolBlock block)
    {
        var raw = block.Arguments.ToString();
        var json = string.IsNullOrWhiteSpace(raw) ? "{}" : raw;

        try
        {
            return new AiToolCall
            {
                Id = block.Id,
                Name = block.Name,
                Arguments = JsonDocument.Parse(json),
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AiUsage CreateUsage(MessageDeltaUsage usage)
    {
        return new AiUsage
        {
            InputTokens = (int)(usage.InputTokens ?? 0),
            OutputTokens = (int)usage.OutputTokens,
            CacheReadTokens = (int)(usage.CacheReadInputTokens ?? 0),
            CacheCreationTokens = (int)(usage.CacheCreationInputTokens ?? 0),
        };
    }

    private sealed class ReplayBlock
    {
        public required string Type { get; init; }

        public StringBuilder? Thinking { get; init; }

        public StringBuilder? Signature { get; init; }

        public string? Data { get; init; }

        public string? ToolUseId { get; init; }
    }

    private sealed record PendingToolBlock
    {
        public long Index { get; init; }

        public required string Id { get; init; }

        public required string Name { get; init; }

        public required StringBuilder Arguments { get; init; }
    }
}
