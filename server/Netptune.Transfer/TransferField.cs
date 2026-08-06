namespace Netptune.Transfer;

public sealed record TransferField
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required TransferValueType ValueType { get; init; }

    public bool IsCollection { get; init; }

    public bool IsRequiredForImport { get; init; }

    public bool IsExportedByDefault { get; init; }

    public string? RefType { get; init; }

    public IReadOnlyList<string> Synonyms { get; init; } = [];

    public string? Example { get; init; }
}
