using Netptune.Transfer.Enums;
using System.Collections.Frozen;
using System.Globalization;
using System.Runtime.CompilerServices;

using ClosedXML.Excel;

using Netptune.Transfer.Services;
using Netptune.Transfer.Import;

namespace Netptune.Import;

public sealed class XlsxImportSourceReader : IImportSourceReader
{
    public IReadOnlySet<ImportSourceKind> Kinds { get; } = new[] { ImportSourceKind.Xlsx }.ToFrozenSet();

    public bool CanRead(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension is ".xlsx" or ".xlsm";
    }

    public async Task<ImportSourceProfile> Profile(Stream source, ImportReadOptions options, CancellationToken cancellationToken = default)
    {
        var sheet = await ReadSheet(source, options, cancellationToken);
        var profiler = new ImportColumnProfiler();
        var isFirst = true;

        foreach (var row in sheet.Rows)
        {
            if (isFirst)
            {
                profiler.SetHeaders(ImportColumnProfiler.HeaderNames(row, options.HasHeaderRow));
                isFirst = false;

                if (options.HasHeaderRow)
                {
                    continue;
                }
            }

            profiler.Add(row);
        }

        return new ImportSourceProfile
        {
            Kind = ImportSourceKind.Xlsx,
            HasHeaderRow = options.HasHeaderRow,
            SheetNames = sheet.SheetNames,
            SelectedSheet = sheet.SelectedSheet,
            EstimatedRowCount = profiler.RowCount,
            Columns = profiler.ToColumns(),
        };
    }

    public async IAsyncEnumerable<ImportRow> ReadRows(
        Stream source,
        ImportReadOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sheet = await ReadSheet(source, options, cancellationToken);

        foreach (var row in sheet.Rows)
        {
            yield return row;
        }
    }

    // ClosedXML has no async API and no way to stream a sheet, so the workbook is opened once on a
    // worker thread and the whole sheet comes back with it. Opening it again for the sheet names, as
    // profiling used to, parses the file twice.
    private static Task<SheetContent> ReadSheet(Stream source, ImportReadOptions options, CancellationToken cancellationToken)
    {
        return Task.Run(() => ReadSheetCore(source, options, cancellationToken), cancellationToken);
    }

    private static SheetContent ReadSheetCore(Stream source, ImportReadOptions options, CancellationToken cancellationToken)
    {
        source.Seek(0, SeekOrigin.Begin);

        using var workbook = new XLWorkbook(source);

        var sheetNames = workbook.Worksheets.Select(sheet => sheet.Name).ToList();
        var selectedSheet = ResolveSheetName(options.SelectedSheet, sheetNames);
        var worksheet = workbook.Worksheets.FirstOrDefault(sheet => sheet.Name == selectedSheet);
        var range = worksheet?.RangeUsed();

        if (range is null)
        {
            return new SheetContent(sheetNames, selectedSheet, []);
        }

        var columnCount = range.ColumnCount();
        var rows = new List<ImportRow>();
        var rowNumber = 0;

        foreach (var row in range.Rows())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var values = new string?[columnCount];

            for (var index = 0; index < columnCount; index++)
            {
                values[index] = ReadCell(row.Cell(index + 1));
            }

            rowNumber++;

            rows.Add(new ImportRow
            {
                RowNumber = rowNumber,
                Values = values,
            });
        }

        return new SheetContent(sheetNames, selectedSheet, rows);
    }

    private static string? ResolveSheetName(string? requested, IReadOnlyList<string> sheetNames)
    {
        if (sheetNames.Count == 0)
        {
            return null;
        }

        var matched = sheetNames.FirstOrDefault(name =>
            string.Equals(name, requested, StringComparison.OrdinalIgnoreCase));

        return matched ?? sheetNames[0];
    }

    private static string? ReadCell(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var dateTime))
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }

        return cell.GetFormattedString();
    }

    private sealed record SheetContent(List<string> SheetNames, string? SelectedSheet, List<ImportRow> Rows);
}
