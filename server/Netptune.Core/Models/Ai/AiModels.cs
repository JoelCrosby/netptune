using Netptune.Core.Enums;

namespace Netptune.Core.Models.Ai;

public sealed record AiModelOption
{
    public AiProvider Provider { get; init; }

    public required string Id { get; init; }

    public required string Label { get; init; }

    public bool IsDefault { get; init; }

    public IReadOnlyList<AiEffort> Efforts { get; init; } = [];
}

public static class AiModels
{
    public const string AnthropicDefault = "claude-opus-5";
    public const string OpenAiDefault = "gpt-5.6-sol";

    public static readonly IReadOnlyList<AiEffort> AllEfforts = Enum.GetValues<AiEffort>();

    private static readonly IReadOnlyList<AiEffort> OpenAiEfforts = [AiEffort.Low, AiEffort.Medium, AiEffort.High];

    public const string AnthropicTitleModel = "claude-haiku-4-5";
    public const string OpenAiTitleModel = "gpt-5.6-luna";

    public static readonly IReadOnlyList<AiModelOption> Catalog =
    [
        new()
        {
            Provider = AiProvider.Anthropic,
            Id = AnthropicDefault,
            Label = "Claude Opus 5",
            IsDefault = true,
            Efforts = AllEfforts,
        },
        new()
        {
            Provider = AiProvider.Anthropic,
            Id = "claude-sonnet-5",
            Label = "Claude Sonnet 5",
            Efforts = AllEfforts,
        },
        new()
        {
            Provider = AiProvider.Anthropic,
            Id = "claude-haiku-4-5",
            Label = "Claude Haiku 4.5",
        },
        new()
        {
            Provider = AiProvider.OpenAi,
            Id = OpenAiDefault,
            Label = "GPT-5.6 Sol",
            IsDefault = true,
            Efforts = OpenAiEfforts,
        },
        new()
        {
            Provider = AiProvider.OpenAi,
            Id = "gpt-5.6-terra",
            Label = "GPT-5.6 Terra",
            Efforts = OpenAiEfforts,
        },
        new()
        {
            Provider = AiProvider.OpenAi,
            Id = OpenAiTitleModel,
            Label = "GPT-5.6 Luna",
            Efforts = OpenAiEfforts,
        },
    ];

    public static string TitleModelFor(AiProvider provider)
    {
        return provider switch
        {
            AiProvider.OpenAi => OpenAiTitleModel,
            _ => AnthropicTitleModel,
        };
    }

    public static bool IsSupported(AiProvider provider, string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        return Catalog.Any(option => option.Provider == provider && option.Id == model);
    }

    public static IReadOnlyList<AiEffort> EffortsFor(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return [];
        }

        var option = Catalog.FirstOrDefault(item => item.Id == model);

        return option?.Efforts ?? [];
    }

    public static AiEffort? ClampEffort(IReadOnlyList<AiEffort> supported, AiEffort desired)
    {
        if (supported.Count == 0)
        {
            return null;
        }

        var isSupported = supported.Contains(desired);

        if (isSupported)
        {
            return desired;
        }

        var lower = supported.Where(effort => effort < desired).ToList();
        var hasLower = lower.Count > 0;

        return hasLower ? lower.Max() : supported.Min();
    }

    public static AiProvider? ProviderFor(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        var option = Catalog.FirstOrDefault(item => item.Id == model);

        return option?.Provider;
    }
}
