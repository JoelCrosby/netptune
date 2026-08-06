namespace Netptune.Transfer;

public sealed record TransferEntityClassification
{
    public required Type EntityType { get; init; }

    public required TransferEntityDisposition Disposition { get; init; }

    public string? RedactionKey { get; init; }
}
