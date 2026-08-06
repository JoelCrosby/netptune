using Netptune.Transfer.Enums;

namespace Netptune.Transfer.Import;

public sealed record ImportSourceColumn
{
    public required int Index { get; init; }

    public required string Name { get; init; }

    public required TransferValueType InferredType { get; init; }

    public int NonEmptyCount { get; init; }

    public int DistinctCount { get; init; }

    public List<string> SampleValues { get; init; } = [];
}

public sealed record ImportSourceProfile
{
    public const int MaxSampleValues = 10;

    public const int MaxSampleLength = 120;

    public required ImportSourceKind Kind { get; init; }

    public string? Encoding { get; init; }

    public char? Delimiter { get; init; }

    public bool HasHeaderRow { get; init; }

    public List<string> SheetNames { get; init; } = [];

    public string? SelectedSheet { get; init; }

    public string? VendorProfile { get; init; }

    public long EstimatedRowCount { get; init; }

    public List<ImportSourceColumn> Columns { get; init; } = [];
}

public sealed record ImportRow
{
    public required int RowNumber { get; init; }

    public required IReadOnlyList<string?> Values { get; init; }
}
