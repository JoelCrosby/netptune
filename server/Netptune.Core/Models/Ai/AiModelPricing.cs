namespace Netptune.Core.Models.Ai;

public sealed record AiModelRate
{
    public decimal InputPerMillion { get; init; }

    public decimal OutputPerMillion { get; init; }

    public decimal CacheReadPerMillion { get; init; }

    public decimal CacheWritePerMillion { get; init; }
}

public static class AiModelPricing
{
    private const decimal TokensPerMillion = 1_000_000m;

    private static readonly IReadOnlyDictionary<string, AiModelRate> Rates =
        new Dictionary<string, AiModelRate>(StringComparer.OrdinalIgnoreCase)
        {
            ["claude-opus-5"] = new()
            {
                InputPerMillion = 5m,
                OutputPerMillion = 25m,
                CacheReadPerMillion = 0.5m,
                CacheWritePerMillion = 6.25m,
            },
            ["claude-sonnet-5"] = new()
            {
                InputPerMillion = 3m,
                OutputPerMillion = 15m,
                CacheReadPerMillion = 0.3m,
                CacheWritePerMillion = 3.75m,
            },
            ["claude-haiku-4-5"] = new()
            {
                InputPerMillion = 1m,
                OutputPerMillion = 5m,
                CacheReadPerMillion = 0.1m,
                CacheWritePerMillion = 1.25m,
            },
            ["gpt-5.6-sol"] = new()
            {
                InputPerMillion = 5m,
                OutputPerMillion = 30m,
                CacheReadPerMillion = 0.5m,
                CacheWritePerMillion = 5m,
            },
            ["gpt-5.6-terra"] = new()
            {
                InputPerMillion = 2m,
                OutputPerMillion = 12m,
                CacheReadPerMillion = 0.2m,
                CacheWritePerMillion = 2m,
            },
            ["gpt-5.6-luna"] = new()
            {
                InputPerMillion = 0.2m,
                OutputPerMillion = 1.2m,
                CacheReadPerMillion = 0.02m,
                CacheWritePerMillion = 0.2m,
            },
        };

    public static AiModelRate? RateFor(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        return Rates.TryGetValue(model, out var rate) ? rate : null;
    }

    public static decimal Cost(
        string? model,
        int inputTokens,
        int outputTokens,
        int cacheReadTokens,
        int cacheCreationTokens)
    {
        var rate = RateFor(model);

        if (rate is null)
        {
            return 0m;
        }

        var total =
            inputTokens * rate.InputPerMillion +
            outputTokens * rate.OutputPerMillion +
            cacheReadTokens * rate.CacheReadPerMillion +
            cacheCreationTokens * rate.CacheWritePerMillion;

        return decimal.Round(total / TokensPerMillion, 6, MidpointRounding.AwayFromZero);
    }
}
