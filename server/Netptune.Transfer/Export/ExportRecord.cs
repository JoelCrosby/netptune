namespace Netptune.Transfer.Export;

public sealed record ExportRecord
{
    public required EntityRef Ref { get; init; }

    public required IReadOnlyDictionary<string, object?> Values { get; init; }
}
