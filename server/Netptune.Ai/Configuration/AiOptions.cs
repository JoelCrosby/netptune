using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Ai.Configuration;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string AnthropicModel { get; set; } = AiModels.AnthropicDefault;

    public string OpenAiModel { get; set; } = AiModels.OpenAiDefault;

    public AiEffort DefaultEffort { get; set; } = AiEffort.Medium;

    public bool GenerateTitles { get; set; } = true;

    public int MaxToolIterations { get; set; } = 12;

    public int MaxOutputTokens { get; set; } = 16000;

    public int MaxToolResultCharacters { get; set; } = 32000;

    public int MaxHistoryCharacters { get; set; } = 120000;

    public AiWebOptions Web { get; set; } = new();
}

public sealed class AiWebOptions
{
    public int MaxDocumentCharacters { get; set; } = 200000;

    public long MaxResponseBytes { get; set; } = 5 * 1024 * 1024;

    public int MaxRedirects { get; set; } = 5;

    public int TimeoutSeconds { get; set; } = 20;

    public int RetentionHours { get; set; } = 24;

    public int DefaultPageCharacters { get; set; } = 6000;

    public int MaxPageCharacters { get; set; } = 20000;

    public int MaxSearchResults { get; set; } = 10;
}
