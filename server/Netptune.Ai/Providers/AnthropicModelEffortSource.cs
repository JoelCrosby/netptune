using Anthropic;
using Anthropic.Models.Models;

using Netptune.Core.Enums;

namespace Netptune.Ai.Providers;

public sealed class AnthropicModelEffortSource : IAiModelEffortSource
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    public AiProvider Provider => AiProvider.Anthropic;

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<AiEffort>>> GetEfforts(
        string apiKey,
        CancellationToken cancellationToken)
    {
        using var client = new AnthropicClient { ApiKey = apiKey, Timeout = RequestTimeout, MaxRetries = 0 };
        var page = await client.Models.List(new ModelListParams { Limit = 1000 }, cancellationToken);
        var efforts = new Dictionary<string, IReadOnlyList<AiEffort>>();

        await foreach (var model in page.Paginate(cancellationToken))
        {
            efforts[model.ID] = ReadEfforts(model.Capabilities?.Effort);
        }

        return efforts;
    }

    private static List<AiEffort> ReadEfforts(EffortCapability? capability)
    {
        var supportsEffort = capability?.Supported ?? false;

        if (!supportsEffort)
        {
            return [];
        }

        var levels = new (CapabilitySupport? Support, AiEffort Effort)[]
        {
            (capability!.Low, AiEffort.Low),
            (capability.Medium, AiEffort.Medium),
            (capability.High, AiEffort.High),
            (capability.Xhigh, AiEffort.XHigh),
            (capability.Max, AiEffort.Max),
        };

        return levels
            .Where(level => level.Support?.Supported ?? false)
            .Select(level => level.Effort)
            .ToList();
    }
}
