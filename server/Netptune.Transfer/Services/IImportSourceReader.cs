using Netptune.Transfer.Enums;
using Netptune.Transfer.Import;

namespace Netptune.Transfer.Services;

public sealed record ImportReadOptions
{
    public char? Delimiter { get; init; }

    public bool HasHeaderRow { get; init; } = true;

    public string? SelectedSheet { get; init; }

    // Name of the JSON property holding the rows. Null picks the richest object array.
    public string? RowSelector { get; init; }
}

public interface IImportSourceReader
{
    IReadOnlySet<ImportSourceKind> Kinds { get; }

    bool CanRead(string fileName);

    Task<ImportSourceProfile> Profile(Stream source, ImportReadOptions options, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ImportRow> ReadRows(Stream source, ImportReadOptions options, CancellationToken cancellationToken = default);
}
