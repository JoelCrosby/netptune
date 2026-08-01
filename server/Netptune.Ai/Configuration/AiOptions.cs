using Netptune.Core.Models.Ai;

namespace Netptune.Ai.Configuration;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string AnthropicModel { get; set; } = AiModels.AnthropicDefault;

    public string OpenAiModel { get; set; } = AiModels.OpenAiDefault;

    public int MaxToolIterations { get; set; } = 12;

    public int MaxOutputTokens { get; set; } = 16000;

    public int MaxToolResultCharacters { get; set; } = 32000;
}
