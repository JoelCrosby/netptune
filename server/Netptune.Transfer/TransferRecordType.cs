namespace Netptune.Transfer;

public sealed record TransferRecordType
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public bool IsStandaloneExportable { get; init; }

    public IReadOnlyList<TransferField> Fields { get; init; } = [];
}
