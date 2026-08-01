using Netptune.Core.Enums;

namespace Netptune.Core.Models.Ai;

public sealed record AiModelOption
{
    public AiProvider Provider { get; init; }

    public required string Id { get; init; }

    public required string Label { get; init; }

    public bool IsDefault { get; init; }
}

public static class AiModels
{
    public const string AnthropicDefault = "claude-opus-5";
    public const string OpenAiDefault = "gpt-5.2";

    public static readonly IReadOnlyList<AiModelOption> Catalog =
    [
        new()
        {
            Provider = AiProvider.Anthropic,
            Id = AnthropicDefault,
            Label = "Claude Opus 5",
            IsDefault = true,
        },
        new()
        {
            Provider = AiProvider.Anthropic,
            Id = "claude-sonnet-5",
            Label = "Claude Sonnet 5",
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
            Label = "GPT-5.2",
            IsDefault = true,
        },
        new()
        {
            Provider = AiProvider.OpenAi,
            Id = "gpt-5.2-mini",
            Label = "GPT-5.2 mini",
        },
        new()
        {
            Provider = AiProvider.OpenAi,
            Id = "gpt-5.1",
            Label = "GPT-5.1",
        },
    ];

    public static bool IsSupported(AiProvider provider, string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        return Catalog.Any(option => option.Provider == provider && option.Id == model);
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
