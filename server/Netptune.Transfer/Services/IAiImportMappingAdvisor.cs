using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Transfer.Import;

namespace Netptune.Transfer.Services;

public sealed record AiImportMappingRequest
{
    public AiProvider Provider { get; init; }

    public required string ApiKey { get; init; }

    public required string RecordType { get; init; }

    public required ImportSourceProfile Profile { get; init; }

    public required ImportMappingModel HeuristicMapping { get; init; }

    public ImportSuggestionVocabulary? Vocabulary { get; init; }

    // When false the model sees column names and inferred types only. Real cell values never leave
    // the system — see the workspace's AllowAssistantDataSampling setting.
    public bool AllowDataSampling { get; init; } = true;
}

public sealed record AiImportMappingResult
{
    public required ImportMappingModel Mapping { get; init; }

    public int DiscardedBindings { get; init; }

    public IReadOnlyList<string> DiscardReasons { get; init; } = [];

    public string? Notes { get; init; }

    public AiUsage Usage { get; init; } = new();
}

public interface IAiImportMappingAdvisor
{
    Task<AiImportMappingResult> Suggest(AiImportMappingRequest request, CancellationToken cancellationToken);
}
