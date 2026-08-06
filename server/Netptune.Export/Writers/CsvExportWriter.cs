using Netptune.Transfer.Enums;
using System.Globalization;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Export;

namespace Netptune.Export.Writers;

public sealed class CsvExportWriter : IExportWriter
{
    public CsvExportWriter(ExportFormat format)
    {
        Format = format;
    }

    public ExportFormat Format { get; }

    public string ContentType => Format == ExportFormat.Tsv ? "text/tab-separated-values" : "text/csv";

    public string FileExtension => Format == ExportFormat.Tsv ? "tsv" : "csv";

    public async Task<long> Write(ExportWriteRequest request, Stream output, CancellationToken cancellationToken = default)
    {
        var options = request.Options;
        var formatter = new ExportValueFormatter(options);
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ResolveDelimiter(options).ToString(),
            HasHeaderRecord = options.IncludeHeaderRow,
        };

        await using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        await using var csv = new CsvWriter(writer, configuration);

        if (options.IncludeHeaderRow)
        {
            foreach (var field in request.Fields)
            {
                csv.WriteField(field.Name);
            }

            await csv.NextRecordAsync();
        }

        var rowCount = 0L;

        await foreach (var record in request.Records.WithCancellation(cancellationToken))
        {
            rowCount += await WriteRecord(csv, record, request.Fields, formatter, options);
        }

        await csv.FlushAsync();
        await writer.FlushAsync(cancellationToken);

        return rowCount;
    }

    private static async Task<long> WriteRecord(
        CsvWriter csv,
        ExportRecord record,
        IReadOnlyList<TransferField> fields,
        ExportValueFormatter formatter,
        ExportOptionsModel options)
    {
        var rows = BuildRows(record, fields, formatter, options);

        foreach (var row in rows)
        {
            foreach (var value in row)
            {
                csv.WriteField(formatter.Format(value));
            }

            await csv.NextRecordAsync();
        }

        return rows.Count;
    }

    // With expansion on, a record becomes the cross product of its collection fields — a task with
    // three assignees and four tags is twelve rows — so the row count grows multiplicatively with the
    // number of collection fields selected, not just with their length.
    private static List<List<object?>> BuildRows(
        ExportRecord record,
        IReadOnlyList<TransferField> fields,
        ExportValueFormatter formatter,
        ExportOptionsModel options)
    {
        if (!options.ExpandCollectionsToRows)
        {
            return [fields.Select(field => record.Values.GetValueOrDefault(field.Key)).ToList()];
        }

        var rows = new List<List<object?>> { new() };

        foreach (var field in fields)
        {
            var value = record.Values.GetValueOrDefault(field.Key);
            IReadOnlyList<object?> alternatives = field.IsCollection ? formatter.Expand(value) : [value];
            var expanded = new List<List<object?>>(rows.Count * alternatives.Count);

            foreach (var row in rows)
            {
                foreach (var alternative in alternatives)
                {
                    expanded.Add([.. row, alternative]);
                }
            }

            rows = expanded;
        }

        return rows;
    }

    private char ResolveDelimiter(ExportOptionsModel options)
    {
        if (Format == ExportFormat.Tsv)
        {
            return '\t';
        }

        return options.Delimiter;
    }
}
