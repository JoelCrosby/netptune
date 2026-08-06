namespace Netptune.Transfer.Import;

// The raw shape the model is asked to return. Nothing here is trusted until validated.
public sealed record AiMappingProposal
{
    public List<AiMappingProposalBinding> Bindings { get; init; } = [];

    public List<int> Unmapped { get; init; } = [];

    public string? Notes { get; init; }
}

public sealed record AiMappingProposalBinding
{
    public string? FieldKey { get; init; }

    public int? ColumnIndex { get; init; }

    public List<AiMappingProposalTransform> Transforms { get; init; } = [];

    public Dictionary<string, string> ValueMap { get; init; } = [];

    public double Confidence { get; init; }

    public string? Rationale { get; init; }
}

public sealed record AiMappingProposalTransform
{
    public string? Kind { get; init; }

    public string? Argument { get; init; }
}
